using System.Security.Cryptography;
using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;
using UniSwap.Domain.ValueObjects;

namespace UniSwap.Application.Users;

public sealed record ProfileResponse(Guid Id, string Name, string Email, string Role)
{
    public static ProfileResponse From(User user) => new(user.Id, user.Name, user.Email.Value, user.Role.ToString());
}

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
public sealed record AuthenticatedAccount(Guid Id, UserRole Role);
public sealed record AccountSession(string TokenHash, Guid UserId, DateTimeOffset ExpiresAtUtc);

public interface IAccountRepository
{
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct);
    void Add(User user);
    Task SaveNewAccountAsync(CancellationToken ct);
}

public interface IAccountSessions
{
    void Add(AccountSession session);
    Task<AuthenticatedAccount?> ResolveAsync(string tokenHash, DateTimeOffset now, CancellationToken ct);
    Task RevokeAsync(string tokenHash, CancellationToken ct);
}

public interface IAccountPasswords
{
    string Hash(User user, string password);
    bool Verify(User? user, string password);
}

public sealed class Accounts(IAccountRepository users, IAccountSessions sessions,
    IAccountPasswords passwords, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<ProfileResponse> RegisterAsync(string name, string email, string password, CancellationToken ct) =>
        CreateAsync(name, NormalizeEmail(email), password, UserRole.User, ct);

    public async Task EnsureAdminAsync(string name, string email, string password, CancellationToken ct)
    {
        var normalized = NormalizeEmail(email);
        var existing = await users.FindByEmailAsync(normalized, ct);
        if (existing is null)
            await CreateAsync(name, normalized, password, UserRole.Admin, ct);
        else if (existing.Role != UserRole.Admin)
            throw new InvalidOperationException("The configured administrator email already belongs to a regular account.");
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(NormalizeEmail(email), ct);
        if (!passwords.Verify(user, password) || user is null) return null;

        var secret = RandomNumberGenerator.GetBytes(32);
        var expiresAt = clock.GetUtcNow().AddHours(12);
        sessions.Add(new AccountSession(HashToken(secret), user.Id, expiresAt));
        await unitOfWork.SaveChangesAsync(ct);
        return new LoginResponse(Convert.ToHexStringLower(secret), expiresAt);
    }

    public Task<AuthenticatedAccount?> AuthenticateAsync(string tokenHash, CancellationToken ct) =>
        sessions.ResolveAsync(tokenHash, clock.GetUtcNow(), ct);

    public Task LogoutAsync(string sessionHash, CancellationToken ct) =>
        sessions.RevokeAsync(sessionHash, ct);

    public async Task<ProfileResponse> GetProfileAsync(Guid id, CancellationToken ct) =>
        ProfileResponse.From(await RequireUserAsync(id, ct));

    public async Task<ProfileResponse> UpdateProfileAsync(Guid id, string name, CancellationToken ct)
    {
        var user = await RequireUserAsync(id, ct);
        user.UpdateName(name);
        await unitOfWork.SaveChangesAsync(ct);
        return ProfileResponse.From(user);
    }

    private async Task<ProfileResponse> CreateAsync(string name, string email, string password, UserRole role, CancellationToken ct)
    {
        if (await users.FindByEmailAsync(email, ct) is not null) throw new RequestConflictException(ConflictCodes.EmailAlreadyRegistered);
        var user = new User(name, new Email(email), role);
        user.SetPasswordHash(passwords.Hash(user, password));
        users.Add(user);
        await users.SaveNewAccountAsync(ct);
        return ProfileResponse.From(user);
    }

    private async Task<User> RequireUserAsync(Guid id, CancellationToken ct) =>
        await users.FindByIdAsync(id, ct) ?? throw new ResourceNotFoundException();

    public static string HashToken(ReadOnlySpan<byte> token) => Convert.ToHexStringLower(SHA256.HashData(token));

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

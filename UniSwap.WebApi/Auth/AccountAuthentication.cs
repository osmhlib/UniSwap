using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UniSwap.Application.Users;
using UniSwap.Domain.Entities;
using UniSwap.Domain.ValueObjects;

namespace UniSwap.WebApi.Auth;

public static class AccountIdentity
{
    public const string Scheme = "Bearer";
    public const string AdminPolicy = "Administrator";
    public const string SessionClaim = "uniswap:session";

    public static Guid UserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, Accounts accounts) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var header) ||
            !string.Equals(header.Scheme, AccountIdentity.Scheme, StringComparison.OrdinalIgnoreCase) ||
            header.Parameter is not { Length: 64 } token || !token.All(Uri.IsHexDigit))
            return AuthenticateResult.NoResult();

        var hash = Accounts.HashToken(Convert.FromHexString(token));
        var user = await accounts.AuthenticateAsync(hash, Context.RequestAborted);
        if (user is null) return AuthenticateResult.Fail("Invalid or expired session.");

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(AccountIdentity.SessionClaim, hash)
        ], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = AccountIdentity.Scheme;
        return base.HandleChallengeAsync(properties);
    }
}

public sealed class AccountPasswordHasher(IPasswordHasher<User> hasher) : IAccountPasswords
{
    private static readonly User DummyUser = new("Unknown user", new Email("unknown@invalid.example"));
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(DummyUser, Guid.NewGuid().ToString());

    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public bool Verify(User? user, string password)
    {
        var result = hasher.VerifyHashedPassword(user ?? DummyUser, user?.PasswordHash ?? DummyHash, password);
        return user?.PasswordHash is not null && result != PasswordVerificationResult.Failed;
    }
}

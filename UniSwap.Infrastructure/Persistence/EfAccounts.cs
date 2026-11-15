using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniSwap.Application.Common;
using UniSwap.Application.Users;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Persistence;

public sealed class EfAccounts(UniSwapDbContext db) : IAccountRepository, IAccountSessions
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(x => EF.Property<string>(x, "NormalizedEmail") == normalizedEmail, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);

    public void Add(User user) => db.Users.Add(user);

    public async Task SaveNewAccountAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Users_NormalizedEmail" })
        {
            throw new RequestConflictException(ConflictCodes.EmailAlreadyRegistered);
        }
    }

    public void Add(AccountSession session) => db.AccountSessions.Add(session);

    public Task<AuthenticatedAccount?> ResolveAsync(string tokenHash, DateTimeOffset now, CancellationToken ct) =>
        (from session in db.AccountSessions
         join user in db.Users.AsNoTracking() on session.UserId equals user.Id
         where session.TokenHash == tokenHash && session.ExpiresAtUtc > now
         select new AuthenticatedAccount(user.Id, user.Role)).SingleOrDefaultAsync(ct);

    public async Task RevokeAsync(string tokenHash, CancellationToken ct) =>
        await db.AccountSessions.Where(x => x.TokenHash == tokenHash).ExecuteDeleteAsync(ct);
}

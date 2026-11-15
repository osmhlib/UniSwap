using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Dtos;
using UniSwap.Application.Moderation;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Application.Users;
using UniSwap.Domain.Enums;

namespace UniSwap.Infrastructure.Persistence;

public sealed class EfModerationCatalog(UniSwapDbContext db) : IModerationCatalog
{
    public async Task<ListingPage> ListListingsAsync(ListingStatus status, int page, int pageSize, CancellationToken ct)
    {
        var listings = db.Listings.AsNoTracking().Where(x => x.Status == status);
        var count = await listings.CountAsync(ct);
        var rows = await listings.OrderByDescending(x => x.PublishedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new ListingPage(rows.Select(ListingDto.From).ToArray(), page, pageSize, count);
    }

    public async Task<IReadOnlyList<ProfileResponse>> ListUsersAsync(int page, int pageSize, CancellationToken ct)
    {
        var rows = await db.Users.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Name, x.Email, x.Role }).ToListAsync(ct);
        return rows.Select(x => new ProfileResponse(x.Id, x.Name, x.Email.Value, x.Role.ToString())).ToArray();
    }
}

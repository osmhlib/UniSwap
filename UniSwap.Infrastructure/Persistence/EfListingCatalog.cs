using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Dtos;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;

namespace UniSwap.Infrastructure.Persistence;

public sealed class EfListingCatalog(UniSwapDbContext db) : IListingCatalog
{
    public Task<ListingPage> BrowseAsync(ListingQuery query, CancellationToken ct)
    {
        var listings = db.Listings.AsNoTracking().Where(x => x.Status == ListingStatus.Published ||
            x.Status == ListingStatus.Reserved || x.Status == ListingStatus.InExchange);
        if (query.Available is true) listings = listings.Where(x => x.Status == ListingStatus.Published);
        if (query.Available is false) listings = listings.Where(x => x.Status != ListingStatus.Published);
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var pattern = EscapePattern(query.Category);
            listings = listings.Where(x => EF.Functions.ILike(x.Category, pattern, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(query.Condition))
        {
            var pattern = EscapePattern(query.Condition);
            listings = listings.Where(x => EF.Functions.ILike(x.Condition, pattern, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + EscapePattern(query.Search) + "%";
            listings = listings.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") ||
                EF.Functions.ILike(x.Description, pattern, "\\"));
        }

        var ordered = query.OldestFirst
            ? listings.OrderBy(x => x.PublishedAtUtc)
            : listings.OrderByDescending(x => x.PublishedAtUtc);
        return PageAsync(ordered.ThenBy(x => x.Id), query.Page, query.PageSize, ct);
    }

    public Task<ListingPage> BrowseOwnedAsync(Guid ownerId, int page, int pageSize, CancellationToken ct) =>
        PageAsync(db.Listings.AsNoTracking().Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id), page, pageSize, ct);

    private static async Task<ListingPage> PageAsync(IOrderedQueryable<Listing> query, int page, int size, CancellationToken ct)
    {
        var count = await query.CountAsync(ct);
        var rows = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new ListingPage(rows.Select(ListingDto.From).ToArray(), page, size, count);
    }

    private static string EscapePattern(string value) =>
        value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

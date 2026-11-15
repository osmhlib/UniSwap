using UniSwap.Domain.Enums;
using UniSwap.Application.UseCases.Exchanges;
using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Persistence;

public sealed class EfListingRepository(UniSwapDbContext db) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(Guid id) => db.Listings.SingleOrDefaultAsync(x => x.Id == id);
    public Task<ListingStatus?> GetStatusAsync(Guid id) =>
        db.Listings.Where(x => x.Id == id).Select(x => (ListingStatus?)x.Status).SingleOrDefaultAsync();

    public async Task AddAsync(Listing listing) => await db.Listings.AddAsync(listing);
    public Task UpdateAsync(Listing listing) => Task.CompletedTask;

    // Force an xmin comparison even when adding a request does not change the listing status.
    public Task RequireUnchangedAsync(Listing listing)
    {
        db.Entry(listing).Property(x => x.Status).IsModified = true;
        return Task.CompletedTask;
    }
}

public sealed class EfExchangeRequestRepository(UniSwapDbContext db) : IExchangeRequestRepository, IExchangeCatalog
{
    public Task<ExchangeRequest?> GetByIdAsync(Guid id) => db.ExchangeRequests.SingleOrDefaultAsync(x => x.Id == id);
    public async Task AddAsync(ExchangeRequest request) => await db.ExchangeRequests.AddAsync(request);
    public Task UpdateAsync(ExchangeRequest request) => Task.CompletedTask;

    public async Task<IReadOnlyList<ExchangeListItem>> ListAsync(Guid actorId, bool received, int page,
        int pageSize, CancellationToken ct)
    {
        var rows = await db.ExchangeRequests.AsNoTracking().Where(x => (received ? x.RecipientId : x.RequesterId) == actorId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        if (rows.Count == 0) return [];

        var details = await RequestPageDetails.LoadAsync(db, actorId,
            rows.SelectMany(x => new[] { x.ListingId, x.OfferedListingId }),
            rows.Select(x => received ? x.RequesterId : x.RecipientId), ct);
        return rows.Select(x =>
        {
            var listing = details.Listings.GetValueOrDefault(x.ListingId);
            var offered = details.Listings.GetValueOrDefault(x.OfferedListingId);
            var counterparty = details.Participants[received ? x.RequesterId : x.RecipientId];
            var canAccept = received && x.Status == ExchangeStatus.Pending &&
                listing?.Status == nameof(ListingStatus.Published) && offered?.Status == nameof(ListingStatus.Published);
            return new ExchangeListItem(x.Id, x.ListingId, x.OfferedListingId, x.RequesterId, x.RecipientId,
                x.Status.ToString(), x.CreatedAtUtc, x.UpdatedAtUtc, listing, offered, counterparty, canAccept);
        }).ToArray();
    }
}

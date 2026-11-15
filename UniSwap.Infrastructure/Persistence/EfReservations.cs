using UniSwap.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Reservations;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Persistence;

public sealed class EfReservations(UniSwapDbContext db) : IReservationRepository
{
    public Task<Reservation?> FindAsync(Guid id, CancellationToken ct) =>
        db.Reservations.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ReservationListItem>> ListAsync(Guid actorId, bool received, int page,
        int pageSize, CancellationToken ct)
    {
        var rows = await db.Reservations.AsNoTracking().Where(x => (received ? x.OwnerId : x.RequesterId) == actorId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        if (rows.Count == 0) return [];

        var details = await RequestPageDetails.LoadAsync(db, actorId, rows.Select(x => x.ListingId),
            rows.Select(x => received ? x.RequesterId : x.OwnerId), ct);
        return rows.Select(x =>
        {
            var listing = details.Listings.GetValueOrDefault(x.ListingId);
            var counterparty = details.Participants[received ? x.RequesterId : x.OwnerId];
            var canAccept = received && x.Status == ReservationStatus.Pending &&
                listing?.Status == nameof(ListingStatus.Published);
            return new ReservationListItem(x.Id, x.ListingId, x.OwnerId, x.RequesterId, x.Status.ToString(),
                x.CreatedAtUtc, x.UpdatedAtUtc, listing, counterparty, canAccept);
        }).ToArray();
    }

    public void Add(Reservation reservation) => db.Reservations.Add(reservation);
}

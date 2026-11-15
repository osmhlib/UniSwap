using UniSwap.Application.Dtos;
using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.Reservations;

public record ReservationResponse(Guid Id, Guid ListingId, Guid OwnerId, Guid RequesterId,
    string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public static ReservationResponse From(Reservation reservation) => new(reservation.Id, reservation.ListingId,
        reservation.OwnerId, reservation.RequesterId, reservation.Status.ToString(), reservation.CreatedAtUtc,
        reservation.UpdatedAtUtc);
}

public sealed record ReservationListItem(Guid Id, Guid ListingId, Guid OwnerId, Guid RequesterId,
    string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    RequestListing? Listing, RequestParticipant Counterparty, bool CanAccept)
    : ReservationResponse(Id, ListingId, OwnerId, RequesterId, Status, CreatedAtUtc, UpdatedAtUtc);

public interface IReservationRepository
{
    Task<Reservation?> FindAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ReservationListItem>> ListAsync(Guid actorId, bool received, int page, int pageSize, CancellationToken ct);
    void Add(Reservation reservation);
}

public sealed class ReservationWorkflow(IReservationRepository reservations, IListingRepository listings,
    IUnitOfWork unitOfWork, IDomainEventPublisher events, IListingCache cache)
{
    public async Task<ReservationResponse> CreateAsync(Guid listingId, Guid actorId, CancellationToken ct)
    {
        var listing = await RequireListingAsync(listingId);
        var reservation = new Reservation(listing, actorId);
        reservations.Add(reservation);
        await listings.RequireUnchangedAsync(listing);
        await unitOfWork.SaveChangesAsync(ct);
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationResponse> AcceptAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var reservation = await RequireReservationAsync(id, ct);
        if (reservation.OwnerId != actorId) throw new UnauthorizedAccessException();
        if (reservation.Status != ReservationStatus.Pending)
            throw new RequestConflictException(ConflictCodes.StateConflict);
        var listing = await listings.GetByIdAsync(reservation.ListingId);
        if (listing?.Status != ListingStatus.Published)
            throw new RequestConflictException(ConflictCodes.ListingUnavailable);
        reservation.Accept(actorId, listing);
        await listings.UpdateAsync(listing);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.InvalidateAsync(listing.Id);
        await events.PublishAsync(listing);
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationResponse> RejectAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var reservation = await RequireReservationAsync(id, ct);
        if (reservation.OwnerId != actorId) throw new UnauthorizedAccessException();
        reservation.Reject(actorId);
        await unitOfWork.SaveChangesAsync(ct);
        return ReservationResponse.From(reservation);
    }

    public async Task<ReservationResponse> CancelAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var reservation = await RequireReservationAsync(id, ct);
        if (reservation.RequesterId != actorId) throw new UnauthorizedAccessException();
        reservation.Cancel(actorId);
        await unitOfWork.SaveChangesAsync(ct);
        return ReservationResponse.From(reservation);
    }

    private async Task<Listing> RequireListingAsync(Guid id) =>
        await listings.GetByIdAsync(id) ?? throw new ResourceNotFoundException();

    private async Task<Reservation> RequireReservationAsync(Guid id, CancellationToken ct) =>
        await reservations.FindAsync(id, ct) ?? throw new ResourceNotFoundException();
}

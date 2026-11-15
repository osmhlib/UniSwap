using UniSwap.Domain.Enums;

namespace UniSwap.Domain.Entities;

public sealed class Reservation : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid OwnerId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public Reservation(Listing listing, Guid requesterId)
    {
        ArgumentNullException.ThrowIfNull(listing);
        if (requesterId == Guid.Empty || requesterId == listing.OwnerId) throw new ArgumentException("A different requester is required.", nameof(requesterId));
        if (listing.Status != ListingStatus.Published) throw new InvalidOperationException("Only published listings can be reserved.");
        Id = Guid.NewGuid(); ListingId = listing.Id; RequesterId = requesterId; OwnerId = listing.OwnerId;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow; Status = ReservationStatus.Pending;
    }

    public void Accept(Guid actorId, Listing listing)
    {
        RequirePending();
        if (actorId != OwnerId) throw new UnauthorizedAccessException("Only the owner can accept a reservation.");
        if (listing.Id != ListingId) throw new ArgumentException("The listing does not match the reservation.", nameof(listing));
        listing.Reserve(RequesterId);
        Status = ReservationStatus.Accepted; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid actorId)
    {
        RequirePending();
        if (actorId != OwnerId) throw new UnauthorizedAccessException("Only the owner can reject a reservation.");
        Status = ReservationStatus.Rejected; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(Guid actorId)
    {
        RequirePending();
        if (actorId != RequesterId) throw new UnauthorizedAccessException("Only the requester can cancel a reservation.");
        Status = ReservationStatus.Cancelled; UpdatedAtUtc = DateTime.UtcNow;
    }

    private void RequirePending()
    {
        if (Status != ReservationStatus.Pending) throw new InvalidOperationException("Only pending reservations can be changed.");
    }
}

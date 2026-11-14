namespace UniSwap.Domain.Events;

public sealed record ReservationConfirmed(
    Guid ListingId,
    Guid RequesterId,
    DateTime OccurredOnUtc) : IDomainEvent;

namespace UniSwap.Domain.Events;

public sealed record ExchangeRequestAccepted(
    Guid ExchangeRequestId,
    Guid ListingId,
    Guid RequesterId,
    DateTime OccurredOnUtc) : IDomainEvent;

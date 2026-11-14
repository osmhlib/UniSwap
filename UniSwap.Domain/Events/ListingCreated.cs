namespace UniSwap.Domain.Events;

public sealed record ListingCreated(
    Guid ListingId,
    Guid OwnerId,
    string Title,
    DateTime OccurredOnUtc) : IDomainEvent;

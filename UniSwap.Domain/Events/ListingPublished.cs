namespace UniSwap.Domain.Events;

public sealed record ListingPublished(
    Guid ListingId,
    Guid OwnerId,
    DateTime OccurredOnUtc) : IDomainEvent;

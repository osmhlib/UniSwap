namespace UniSwap.Application.Dtos;

public sealed record ListingDto(Guid Id, Guid OwnerId, string Title, string Description,
    string Category, string Condition, string Status, DateTime CreatedAtUtc, DateTime? PublishedAtUtc);

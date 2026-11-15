using UniSwap.Domain.Entities;

namespace UniSwap.Application.Dtos;

public sealed record ListingDto(Guid Id, Guid OwnerId, string Title, string Description,
    string Category, string Condition, string Status, DateTime CreatedAtUtc, DateTime? PublishedAtUtc,
    string? ModerationReason = null)
{
    public static ListingDto From(Listing listing) => new(listing.Id, listing.OwnerId, listing.Title, listing.Description,
        listing.Category, listing.Condition, listing.Status.ToString(), listing.CreatedAtUtc, listing.PublishedAtUtc,
        listing.ModerationReason);
}

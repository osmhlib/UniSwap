using UniSwap.Application.Dtos;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.UseCases.Listings;

public sealed class GetListing(IListingRepository listings, IListingCache cache)
{
    public async Task<ListingDto?> ExecuteAsync(Guid id)
    {
        var cached = await cache.GetAsync(id);
        if (cached is not null) return cached;
        var listing = await listings.GetByIdAsync(id);
        if (listing is null || listing.Status != ListingStatus.Published)
            return null;

        var dto = new ListingDto(listing.Id, listing.OwnerId, listing.Title, listing.Description,
            listing.Category, listing.Condition, listing.Status.ToString(), listing.CreatedAtUtc, listing.PublishedAtUtc);
        await cache.SetAsync(id, dto);
        return dto;
    }
}

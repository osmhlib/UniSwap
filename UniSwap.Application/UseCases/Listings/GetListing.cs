using UniSwap.Application.Dtos;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.UseCases.Listings;

public sealed class GetListing(IListingRepository listings, IListingCache cache)
{
    public async Task<ListingDto?> ExecuteAsync(Guid id)
    {
        var cached = await cache.GetAsync(id);
        // Cache invalidation is best effort. The database decides if a cached listing is still public.
        if (cached is { Status: nameof(ListingStatus.Published) } &&
            await listings.GetStatusAsync(id) == ListingStatus.Published)
            return cached;


        var listing = await listings.GetByIdAsync(id);
        if (listing is null || listing.Status is not (ListingStatus.Published or ListingStatus.Reserved or ListingStatus.InExchange) || listing.DeletedAtUtc.HasValue)
            return null;

        var dto = ListingDto.From(listing);
        if (listing.Status == ListingStatus.Published) await cache.SetAsync(id, dto);
        return dto;
    }
}

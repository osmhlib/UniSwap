using UniSwap.Application.Interfaces;
using UniSwap.Application.Services;

namespace UniSwap.Application.UseCases.Listings;

public sealed class PublishListing(
    IListingRepository listingRepository,
    DomainEventPublisher eventPublisher)
{
    public async Task ExecuteAsync(Guid listingId)
    {
        var listing = await listingRepository.GetByIdAsync(listingId)
            ?? throw new InvalidOperationException("Listing not found.");

        listing.Publish();
        await listingRepository.UpdateAsync(listing);
        await eventPublisher.PublishAsync(listing);
    }
}

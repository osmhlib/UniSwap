using UniSwap.Application.Interfaces;
using UniSwap.Application.Services;

namespace UniSwap.Application.UseCases.Listings;

public sealed class PublishListing(
    IListingRepository listingRepository,
    IDomainEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    IListingCache cache)
{
    public async Task ExecuteAsync(Guid listingId)
    {
        var listing = await listingRepository.GetByIdAsync(listingId)
            ?? throw new InvalidOperationException("Listing not found.");

        listing.Publish();
        await listingRepository.UpdateAsync(listing);
        await unitOfWork.SaveChangesAsync();
        await cache.InvalidateAsync(listing.Id);
        await eventPublisher.PublishAsync(listing);
    }
}

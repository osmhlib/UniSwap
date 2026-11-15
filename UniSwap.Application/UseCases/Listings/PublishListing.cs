using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;
using UniSwap.Application.Services;

namespace UniSwap.Application.UseCases.Listings;

public sealed class PublishListing(
    IListingRepository listingRepository,
    IDomainEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    IListingCache cache)
{
    public async Task ExecuteAsync(Guid listingId, Guid actorId)
    {
        var listing = await listingRepository.GetByIdAsync(listingId)
            ?? throw new ResourceNotFoundException();

        if (listing.OwnerId != actorId) throw new UnauthorizedAccessException();

        listing.Publish();
        await listingRepository.UpdateAsync(listing);
        await unitOfWork.SaveChangesAsync();
        await cache.InvalidateAsync(listing.Id);
        await eventPublisher.PublishAsync(listing);
    }
}

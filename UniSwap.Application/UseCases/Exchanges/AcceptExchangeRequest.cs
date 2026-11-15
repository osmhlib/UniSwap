using UniSwap.Application.Interfaces;

namespace UniSwap.Application.UseCases.Exchanges;

public sealed class AcceptExchangeRequest(IListingRepository listings, IExchangeRequestRepository requests,
    IDomainEventPublisher events, IUnitOfWork unitOfWork, IListingCache cache)
{
    public async Task ExecuteAsync(Guid exchangeRequestId, Guid actorId)
    {
        var request = await requests.GetByIdAsync(exchangeRequestId) ?? throw new InvalidOperationException("Exchange request not found.");
        var listing = await listings.GetByIdAsync(request.ListingId) ?? throw new InvalidOperationException("Listing not found.");
        var offered = await listings.GetByIdAsync(request.OfferedListingId) ?? throw new InvalidOperationException("Offered listing not found.");
        request.Accept(actorId, listing, offered);
        await requests.UpdateAsync(request);
        await listings.UpdateAsync(listing);
        await listings.UpdateAsync(offered);
        await unitOfWork.SaveChangesAsync();
        await cache.InvalidateAsync(listing.Id);
        await cache.InvalidateAsync(offered.Id);
        await events.PublishAsync([request, listing, offered]);
    }
}

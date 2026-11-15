using UniSwap.Domain.Enums;
using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;

namespace UniSwap.Application.UseCases.Exchanges;

public sealed class AcceptExchangeRequest(IListingRepository listings, IExchangeRequestRepository requests,
    IDomainEventPublisher events, IUnitOfWork unitOfWork, IListingCache cache)
{
    public async Task ExecuteAsync(Guid exchangeRequestId, Guid actorId)
    {
        var request = await requests.GetByIdAsync(exchangeRequestId) ?? throw new ResourceNotFoundException();
        if (request.RecipientId != actorId) throw new UnauthorizedAccessException();
        if (request.Status != ExchangeStatus.Pending)
            throw new RequestConflictException(ConflictCodes.StateConflict);
        var listing = await listings.GetByIdAsync(request.ListingId);
        var offered = await listings.GetByIdAsync(request.OfferedListingId);
        if (listing?.Status != ListingStatus.Published || offered?.Status != ListingStatus.Published)
            throw new RequestConflictException(ConflictCodes.ListingUnavailable);
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

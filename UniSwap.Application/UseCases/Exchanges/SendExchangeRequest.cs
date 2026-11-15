using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Application.UseCases.Exchanges;

public sealed class SendExchangeRequest(IListingRepository listings, IExchangeRequestRepository requests,
    IDomainEventPublisher events, IUnitOfWork unitOfWork)
{
    public async Task<Guid> ExecuteAsync(Guid listingId, Guid offeredListingId, Guid requesterId)
    {
        var listing = await listings.GetByIdAsync(listingId) ?? throw new ResourceNotFoundException();
        var offered = await listings.GetByIdAsync(offeredListingId) ?? throw new ResourceNotFoundException();
        if (offered.OwnerId != requesterId) throw new UnauthorizedAccessException();
        var request = new ExchangeRequest(listing, offered, requesterId);
        await requests.AddAsync(request);
        await listings.RequireUnchangedAsync(listing);
        await listings.RequireUnchangedAsync(offered);
        await unitOfWork.SaveChangesAsync();
        await events.PublishAsync(request);
        return request.Id;
    }
}

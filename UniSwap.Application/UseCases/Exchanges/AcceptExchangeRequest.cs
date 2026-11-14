using UniSwap.Application.Interfaces;
using UniSwap.Application.Services;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.UseCases.Exchanges;

public sealed class AcceptExchangeRequest(
    IListingRepository listingRepository,
    IExchangeRequestRepository exchangeRequestRepository,
    IDomainEventPublisher eventPublisher,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid exchangeRequestId)
    {
        var request = await exchangeRequestRepository.GetByIdAsync(exchangeRequestId)
            ?? throw new InvalidOperationException("Exchange request not found.");
        var listing = await listingRepository.GetByIdAsync(request.ListingId)
            ?? throw new InvalidOperationException("Listing not found.");

        if (request.Status != ExchangeStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be accepted.");
        if (listing.Status != ListingStatus.Available)
            throw new InvalidOperationException("Listing is not available.");

        request.Accept();
        listing.Reserve(request.RequesterId);

        await exchangeRequestRepository.UpdateAsync(request);
        await listingRepository.UpdateAsync(listing);
        await unitOfWork.SaveChangesAsync();
        await eventPublisher.PublishAsync([request, listing]);
    }
}

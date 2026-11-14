using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;
using UniSwap.Application.Services;

namespace UniSwap.Application.UseCases.Exchanges
{
    public class SendExchangeRequest
    {
        private readonly IListingRepository _listingRepository;
        private readonly IExchangeRequestRepository _exchangeRequestRepository;
        private readonly IDomainEventPublisher _eventPublisher;
        private readonly IUnitOfWork _unitOfWork;

        public SendExchangeRequest(
            IListingRepository listingRepository,
            IExchangeRequestRepository exchangeRequestRepository,
            IDomainEventPublisher eventPublisher,
            IUnitOfWork unitOfWork)
        {
            _listingRepository = listingRepository;
            _exchangeRequestRepository = exchangeRequestRepository;
            _eventPublisher = eventPublisher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(
            Guid listingId,
            Guid requesterId)
        {
            var listing = await _listingRepository.GetByIdAsync(listingId);

            if (listing is null)
                throw new InvalidOperationException("Listing not found.");

            if (listing.OwnerId == requesterId)
                throw new InvalidOperationException(
                    "You cannot exchange your own listing.");

            if (listing.Status != ListingStatus.Available)
                throw new InvalidOperationException(
                    "Listing is not available.");

            var request = new ExchangeRequest(
                listingId,
                requesterId);

            await _exchangeRequestRepository.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            await _eventPublisher.PublishAsync(request);

            return request.Id;
        }
    }
}

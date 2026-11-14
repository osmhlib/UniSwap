using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.UseCases.Exchanges
{
    public class SendExchangeRequest
    {
        private readonly IListingRepository _listingRepository;
        private readonly IExchangeRequestRepository _exchangeRequestRepository;

        public SendExchangeRequest(
            IListingRepository listingRepository,
            IExchangeRequestRepository exchangeRequestRepository)
        {
            _listingRepository = listingRepository;
            _exchangeRequestRepository = exchangeRequestRepository;
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

            return request.Id;
        }
    }
}

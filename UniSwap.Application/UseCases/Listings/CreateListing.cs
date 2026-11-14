using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;
using UniSwap.Application.Services;

namespace UniSwap.Application.UseCases.Listings
{
    public class CreateListing
    {
        private readonly IListingRepository _listingRepository;
        private readonly DomainEventPublisher _eventPublisher;

        public CreateListing(IListingRepository listingRepository, DomainEventPublisher eventPublisher)
        {
            _listingRepository = listingRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task<Guid> ExecuteAsync(
            Guid ownerId,
            string title,
            string description)
        {
            var listing = new Listing(ownerId, title, description);

            await _listingRepository.AddAsync(listing);
            await _eventPublisher.PublishAsync(listing);

            return listing.Id;
        }
    }
}

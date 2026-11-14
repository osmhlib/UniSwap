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
        private readonly IDomainEventPublisher _eventPublisher;
        private readonly IUnitOfWork _unitOfWork;

        public CreateListing(IListingRepository listingRepository, IDomainEventPublisher eventPublisher, IUnitOfWork unitOfWork)
        {
            _listingRepository = listingRepository;
            _eventPublisher = eventPublisher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(
            Guid ownerId,
            string title,
            string description)
        {
            var listing = new Listing(ownerId, title, description);

            await _listingRepository.AddAsync(listing);
            await _unitOfWork.SaveChangesAsync();
            await _eventPublisher.PublishAsync(listing);

            return listing.Id;
        }
    }
}

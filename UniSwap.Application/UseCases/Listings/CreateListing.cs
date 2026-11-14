using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Application.UseCases.Listings
{
    public class CreateListing
    {
        private readonly IListingRepository _listingRepository;

        public CreateListing(IListingRepository listingRepository)
        {
            _listingRepository = listingRepository;
        }

        public async Task<Guid> ExecuteAsync(
            Guid ownerId,
            string title,
            string description)
        {
            var listing = new Listing(ownerId, title, description);

            await _listingRepository.AddAsync(listing);

            return listing.Id;
        }
    }
}

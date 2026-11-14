using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Repositories
{
    public class InMemoryListingRepository : IListingRepository
    {
        private readonly List<Listing> _listings = new();

        public Task<Listing?> GetByIdAsync(Guid id)
        {
            var listing = _listings.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(listing);
        }

        public Task AddAsync(Listing listing)
        {
            _listings.Add(listing);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Listing listing)
        {
            return Task.CompletedTask;
        }
    }
}

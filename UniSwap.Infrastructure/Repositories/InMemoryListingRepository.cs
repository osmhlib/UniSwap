using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Repositories;

public sealed class InMemoryListingRepository(InMemoryStore store, InMemoryUnitOfWork unitOfWork) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(Guid id)
    {
        if (unitOfWork.Listings.TryGetValue(id, out var pending))
            return Task.FromResult<Listing?>(pending);
        lock (store.SyncRoot)
            return Task.FromResult(store.Listings.TryGetValue(id, out var listing) ? listing.Copy() : null);
    }

    public Task AddAsync(Listing listing)
    {
        unitOfWork.Listings.Add(listing.Id, listing);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Listing listing)
    {
        unitOfWork.Listings[listing.Id] = listing;
        return Task.CompletedTask;
    }
}

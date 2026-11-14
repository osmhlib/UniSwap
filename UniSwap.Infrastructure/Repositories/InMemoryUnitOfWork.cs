using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Repositories;

public sealed class InMemoryUnitOfWork(InMemoryStore store) : IUnitOfWork
{
    internal Dictionary<Guid, Listing> Listings { get; } = [];
    internal Dictionary<Guid, ExchangeRequest> ExchangeRequests { get; } = [];

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var listings = Listings.ToDictionary(pair => pair.Key, pair => pair.Value.Copy());
        var requests = ExchangeRequests.ToDictionary(pair => pair.Key, pair => pair.Value.Copy());

        lock (store.SyncRoot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var pair in listings)
                store.Listings[pair.Key] = pair.Value;
            foreach (var pair in requests)
                store.ExchangeRequests[pair.Key] = pair.Value;
        }

        Listings.Clear();
        ExchangeRequests.Clear();
        return Task.CompletedTask;
    }
}

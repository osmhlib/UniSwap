using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Repositories;

public sealed class InMemoryStore
{
    internal object SyncRoot { get; } = new();
    internal Dictionary<Guid, Listing> Listings { get; } = [];
    internal Dictionary<Guid, ExchangeRequest> ExchangeRequests { get; } = [];
}

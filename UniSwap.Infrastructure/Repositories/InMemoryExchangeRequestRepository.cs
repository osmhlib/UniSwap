using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Repositories;

public sealed class InMemoryExchangeRequestRepository(InMemoryStore store, InMemoryUnitOfWork unitOfWork) : IExchangeRequestRepository
{
    public Task<ExchangeRequest?> GetByIdAsync(Guid id)
    {
        if (unitOfWork.ExchangeRequests.TryGetValue(id, out var pending))
            return Task.FromResult<ExchangeRequest?>(pending);
        lock (store.SyncRoot)
            return Task.FromResult(store.ExchangeRequests.TryGetValue(id, out var request) ? request.Copy() : null);
    }

    public Task AddAsync(ExchangeRequest request)
    {
        unitOfWork.ExchangeRequests.Add(request.Id, request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ExchangeRequest request)
    {
        unitOfWork.ExchangeRequests[request.Id] = request;
        return Task.CompletedTask;
    }
}

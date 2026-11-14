using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Repositories
{
    public class InMemoryExchangeRequestRepository
    : IExchangeRequestRepository
    {
        private readonly List<ExchangeRequest> _requests = new();

        public Task<ExchangeRequest?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_requests.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(ExchangeRequest request)
        {
            _requests.Add(request);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ExchangeRequest request) => Task.CompletedTask;
    }
}

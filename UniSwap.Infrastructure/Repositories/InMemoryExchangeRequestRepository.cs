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

        public Task AddAsync(ExchangeRequest request)
        {
            _requests.Add(request);
            return Task.CompletedTask;
        }
    }
}

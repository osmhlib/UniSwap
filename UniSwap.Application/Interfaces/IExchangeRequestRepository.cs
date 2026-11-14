using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.Entities;

namespace UniSwap.Application.Interfaces
{
    public interface IExchangeRequestRepository
    {
        Task AddAsync(ExchangeRequest request);
    }
}

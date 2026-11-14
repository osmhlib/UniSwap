using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.Entities;

namespace UniSwap.Application.Interfaces
{
    public interface IListingRepository
    {
        Task<Listing?> GetByIdAsync(Guid id);
        Task AddAsync(Listing listing);
        Task UpdateAsync(Listing listing);
    }
}

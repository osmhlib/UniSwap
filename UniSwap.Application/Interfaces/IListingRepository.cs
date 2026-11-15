using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.Interfaces
{
    public interface IListingRepository
    {
        Task<Listing?> GetByIdAsync(Guid id);
        Task<ListingStatus?> GetStatusAsync(Guid id);
        Task AddAsync(Listing listing);
        Task UpdateAsync(Listing listing);
        Task RequireUnchangedAsync(Listing listing);
    }
}

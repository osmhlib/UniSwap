using UniSwap.Application.Dtos;

namespace UniSwap.Application.Interfaces;

public interface IListingCache
{
    Task<ListingDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetAsync(Guid id, ListingDto listing, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid id, CancellationToken cancellationToken = default);
}

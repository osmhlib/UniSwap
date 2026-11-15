using UniSwap.Application.Common;
using UniSwap.Application.Dtos;
using UniSwap.Application.Interfaces;

namespace UniSwap.Application.UseCases.Listings;

public sealed record ListingQuery(string? Search, string? Category, string? Condition, bool? Available,
    bool OldestFirst, int Page, int PageSize);

public sealed record ListingPage(IReadOnlyList<ListingDto> Items, int Page, int PageSize, int TotalCount);

public interface IListingCatalog
{
    Task<ListingPage> BrowseAsync(ListingQuery query, CancellationToken ct);
    Task<ListingPage> BrowseOwnedAsync(Guid ownerId, int page, int pageSize, CancellationToken ct);
}

public sealed class ListingManagement(IListingRepository listings, IUnitOfWork unitOfWork, IListingCache cache)
{
    public async Task<ListingDto> GetOwnedAsync(Guid id, Guid ownerId)
    {
        var listing = await listings.GetByIdAsync(id);
        if (listing is null || listing.OwnerId != ownerId) throw new ResourceNotFoundException();
        return ListingDto.From(listing);
    }

    public Task<ListingDto> UpdateAsync(Guid id, Guid ownerId, string title, string description, string category,
        string condition, CancellationToken ct) =>
        EditAsync(id, ownerId, listing => listing.Update(title, description, category, condition), ct);

    public async Task DeleteAsync(Guid id, Guid ownerId, CancellationToken ct) =>
        await EditAsync(id, ownerId, listing => listing.Delete(), ct);

    private async Task<ListingDto> EditAsync(Guid id, Guid ownerId, Action<Domain.Entities.Listing> change,
        CancellationToken ct)
    {
        var listing = await listings.GetByIdAsync(id) ?? throw new ResourceNotFoundException();
        if (listing.OwnerId != ownerId) throw new UnauthorizedAccessException();

        change(listing);
        await listings.UpdateAsync(listing);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.InvalidateAsync(id);
        return ListingDto.From(listing);
    }
}

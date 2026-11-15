using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.Listings;

public sealed record ListingImageResponse(Guid Id, string ContentType, long SizeBytes)
{
    internal static ListingImageResponse Map(ListingImage image) => new(image.Id, image.ContentType, image.SizeBytes);
}

public interface IListingImageRepository
{
    Task<IReadOnlyList<ListingImage>> GetAsync(Guid listingId, CancellationToken ct);
    Task<ListingImage?> FindAsync(Guid listingId, Guid imageId, CancellationToken ct);
    void Add(Listing listing, ListingImage image);
    void Remove(Listing listing, ListingImage image);
}

public interface IListingImageStorage
{
    Task<ListingImage> StoreAsync(Guid listingId, Stream input, CancellationToken ct);
    Stream Open(string storageKey);
    void TryDelete(string storageKey);
}

public sealed class ListingImages(IListingRepository listings, IListingImageRepository images,
    IListingImageStorage storage, IUnitOfWork unitOfWork)
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxImages = 5;

    public async Task<IReadOnlyList<ListingImageResponse>> ListAsync(Guid id, Guid? actorId, CancellationToken ct)
    {
        await GetVisibleListingAsync(id, actorId);
        return (await images.GetAsync(id, ct)).Select(ListingImageResponse.Map).ToArray();
    }

    public async Task<(Stream Content, string ContentType)> OpenAsync(Guid id, Guid imageId, Guid? actorId,
        CancellationToken ct)
    {
        await GetVisibleListingAsync(id, actorId);
        var image = await RequireImageAsync(id, imageId, ct);
        return (storage.Open(image.StorageKey), image.ContentType);
    }

    public async Task<ListingImageResponse> UploadAsync(Guid id, Guid actorId, Stream input, CancellationToken ct)
    {
        var listing = await GetEditableListingAsync(id, actorId);
        if ((await images.GetAsync(id, ct)).Count >= MaxImages) throw new RequestConflictException(ConflictCodes.ImageLimitReached);

        var image = await storage.StoreAsync(id, input, ct);
        images.Add(listing, image);
        // A known write conflict rolls back all database changes.
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (RequestConflictException)
        {
            storage.TryDelete(image.StorageKey);
            throw;
        }

        return ListingImageResponse.Map(image);
    }

    public async Task DeleteAsync(Guid id, Guid imageId, Guid actorId, CancellationToken ct)
    {
        var listing = await GetEditableListingAsync(id, actorId);
        var image = await RequireImageAsync(id, imageId, ct);
        images.Remove(listing, image);
        await unitOfWork.SaveChangesAsync(ct);
        storage.TryDelete(image.StorageKey);
    }

    private async Task<Listing> GetEditableListingAsync(Guid id, Guid actorId)
    {
        var listing = await listings.GetByIdAsync(id) ?? throw new ResourceNotFoundException();
        if (listing.OwnerId != actorId) throw new UnauthorizedAccessException();
        listing.EnsureEditable();
        return listing;
    }

    private async Task GetVisibleListingAsync(Guid id, Guid? actorId)
    {
        var listing = await listings.GetByIdAsync(id) ?? throw new ResourceNotFoundException();
        if (listing.OwnerId != actorId && listing.Status is not
            (ListingStatus.Published or ListingStatus.Reserved or ListingStatus.InExchange))
            throw new ResourceNotFoundException();
    }

    private async Task<ListingImage> RequireImageAsync(Guid id, Guid imageId, CancellationToken ct) =>
        await images.FindAsync(id, imageId, ct) ?? throw new ResourceNotFoundException();
}

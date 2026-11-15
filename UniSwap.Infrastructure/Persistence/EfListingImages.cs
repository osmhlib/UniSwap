using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Listings;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Persistence;

public sealed class EfListingImages(UniSwapDbContext db) : IListingImageRepository
{
    public async Task<IReadOnlyList<ListingImage>> GetAsync(Guid listingId, CancellationToken ct) =>
        await db.ListingImages.AsNoTracking().Where(x => x.ListingId == listingId).OrderBy(x => x.Id).ToListAsync(ct);

    public Task<ListingImage?> FindAsync(Guid listingId, Guid imageId, CancellationToken ct) =>
        db.ListingImages.SingleOrDefaultAsync(x => x.ListingId == listingId && x.Id == imageId, ct);

    public void Add(Listing listing, ListingImage image)
    {
        TouchListing(listing);
        db.ListingImages.Add(image);
    }

    public void Remove(Listing listing, ListingImage image)
    {
        TouchListing(listing);
        db.ListingImages.Remove(image);
    }

    // Competing uploads and status changes must update the same xmin-protected row.
    private void TouchListing(Listing listing) =>
        db.Entry(listing).Property<Guid>("ImagesRevision").CurrentValue = Guid.NewGuid();
}

namespace UniSwap.Domain.Entities;

/// <summary>Metadata for an image stored outside the database.</summary>
public sealed class ListingImage
{
    public Guid Id { get; private set; }
    public Guid ListingId { get; private set; }
    public string StorageKey { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }

    private ListingImage() { StorageKey = ContentType = null!; }

    public ListingImage(Guid listingId, string storageKey, string contentType, long sizeBytes)
    {
        if (listingId == Guid.Empty) throw new ArgumentException("Listing is required.", nameof(listingId));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.", nameof(storageKey));
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp")) throw new ArgumentException("Unsupported image type.", nameof(contentType));
        if (sizeBytes is <= 0 or > 5 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        Id = Guid.NewGuid(); ListingId = listingId; StorageKey = storageKey; ContentType = contentType; SizeBytes = sizeBytes;
    }
}

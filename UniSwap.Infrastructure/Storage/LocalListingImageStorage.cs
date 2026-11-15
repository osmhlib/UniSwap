using SkiaSharp;
using UniSwap.Application.Common;
using UniSwap.Application.Listings;
using UniSwap.Domain.Entities;

namespace UniSwap.Infrastructure.Storage;

public sealed class LocalListingImageStorage(string root, Action<Exception> onDeleteFailure) : IListingImageStorage
{
    private const long MaxPixels = 40_000_000;

    public async Task<ListingImage> StoreAsync(Guid listingId, Stream input, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        var key = Guid.NewGuid().ToString("N");
        var path = PathFor(key);
        try
        {
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                81920, FileOptions.Asynchronous);
            var buffer = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (file.Length + read > ListingImages.MaxBytes) throw new InvalidImageUploadException();
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            var size = file.Length;
            file.Position = 0;
            ct.ThrowIfCancellationRequested();
            using var codec = SKCodec.Create(file) ?? throw new InvalidImageUploadException();
            var type = codec.EncodedFormat switch
            {
                SKEncodedImageFormat.Jpeg => "image/jpeg",
                SKEncodedImageFormat.Png => "image/png",
                SKEncodedImageFormat.Webp => "image/webp",
                _ => throw new InvalidImageUploadException()
            };

            var info = codec.Info;
            if ((long)info.Width * info.Height > MaxPixels) throw new InvalidImageUploadException();
            using var pixels = new SKBitmap(info);
            if (codec.GetPixels(info, pixels.GetPixels()) != SKCodecResult.Success)
                throw new InvalidImageUploadException();
            return new ListingImage(listingId, key, type, size);
        }
        catch
        {
            TryDelete(key);
            throw;
        }
    }

    public Stream Open(string storageKey)
    {
        try
        {
            return new FileStream(PathFor(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        }
        catch (FileNotFoundException) { throw new ResourceNotFoundException(); }
        catch (DirectoryNotFoundException) { throw new ResourceNotFoundException(); }
    }

    public void TryDelete(string storageKey)
    {
        try { File.Delete(PathFor(storageKey)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { onDeleteFailure(ex); }
    }

    private string PathFor(string key) => Path.Combine(root, Guid.ParseExact(key, "N").ToString("N"));
}

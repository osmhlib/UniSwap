using SkiaSharp;
using UniSwap.Infrastructure.Storage;

internal static class ImageStorageSmokeTests
{
    public static async Task RunAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"uniswap-image-tests-{Guid.NewGuid():N}");
        var storage = new LocalListingImageStorage(directory, ex => throw new IOException("Test image cleanup failed.", ex));
        using var image = new SKBitmap(2, 2);
        image.Erase(SKColors.Green);
        try
        {
            foreach (var (format, mimeType) in new[]
            {
                (SKEncodedImageFormat.Jpeg, "image/jpeg"),
                (SKEncodedImageFormat.Png, "image/png"),
                (SKEncodedImageFormat.Webp, "image/webp")
            })
            {
                using var encoded = image.Encode(format, 80);
                await using var input = encoded.AsStream();
                var stored = await storage.StoreAsync(Guid.NewGuid(), input, CancellationToken.None);
                using (var result = storage.Open(stored.StorageKey))
                {
                    if (stored.ContentType != mimeType || stored.SizeBytes != encoded.Size || result.Length != encoded.Size)
                        throw new InvalidOperationException($"Image storage did not preserve the encoded {format} file.");
                }
                storage.TryDelete(stored.StorageKey);
            }
        }
        finally
        {
            foreach (var file in Directory.Exists(directory) ? Directory.EnumerateFiles(directory) : [])
                File.Delete(file);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }

        Console.WriteLine("Local storage verified JPEG, PNG and WebP decoding.");
    }
}

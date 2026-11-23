using Microsoft.AspNetCore.Http.Features;
using UniSwap.Application.Listings;

namespace UniSwap.WebApi;

/// <summary>Checks multipart uploads before MVC reads and validates the form.</summary>
public sealed class UploadSizeLimitMiddleware(RequestDelegate next)
{
    public const long MaxRequestBytes = ListingImages.MaxBytes + 64 * 1024;

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problems)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<LimitListingImageUploadAttribute>() is null)
        {
            await next(context);
            return;
        }

        if (context.Request.ContentLength > MaxRequestBytes)
        {
            await WriteTooLargeAsync(context, problems);
            return;
        }

        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
            sizeFeature.MaxRequestBodySize = null;

        var originalBody = context.Request.Body;
        context.Request.Body = new LimitedUploadStream(originalBody, MaxRequestBytes);
        try
        {
            // MVC may otherwise convert a body-size exception into a model validation error.
            if (context.Request.HasFormContentType)
                await context.Request.ReadFormAsync(context.RequestAborted);
            await next(context);
        }
        catch (UploadTooLargeException) when (!context.Response.HasStarted)
        {
            await WriteTooLargeAsync(context, problems);
        }
        catch (InvalidDataException) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context });
        }
        finally
        {
            context.Request.Body = originalBody;
        }
    }

    private static async Task WriteTooLargeAsync(HttpContext context, IProblemDetailsService problems)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context });
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class LimitListingImageUploadAttribute : Attribute;

internal sealed class UploadTooLargeException : Exception;

internal sealed class LimitedUploadStream(Stream inner, long limit) : Stream
{
    private long bytesRead;

    private int Count(int read)
    {
        bytesRead += read;
        if (bytesRead > limit) throw new UploadTooLargeException();
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));
    public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        Count(await inner.ReadAsync(buffer, offset, count, cancellationToken));
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Count(await inner.ReadAsync(buffer, cancellationToken));
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => bytesRead; set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

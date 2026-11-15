using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UniSwap.Application.Dtos;
using UniSwap.Application.Interfaces;

namespace UniSwap.Infrastructure.Caching;

// Memcached text protocol; each operation has its own bounded connection.
public sealed class MemcachedListingCache(string host, int port) : IListingCache
{
    private static string Key(Guid id) => $"uniswap:listing:{id:N}";
    private async Task<string?> ExecuteAsync(string command, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(500));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            using var stream = client.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes(command), timeout.Token);
            using var reader = new StreamReader(stream, Encoding.ASCII);
            return await reader.ReadLineAsync(timeout.Token);
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }

    public async Task<ListingDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(500));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            using var stream = client.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"get {Key(id)}\r\n"), timeout.Token);
            using var reader = new StreamReader(stream, Encoding.ASCII);
            var header = await reader.ReadLineAsync(timeout.Token);
            if (header is null || !header.StartsWith("VALUE ", StringComparison.Ordinal)) return null;
            var value = await reader.ReadLineAsync(timeout.Token);
            if (value is null) return null;
            var dto = JsonSerializer.Deserialize<ListingDto>(Convert.FromBase64String(value));
            return dto?.Id == id && dto.Status == "Published" ? dto : null;
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException or FormatException or JsonException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }

    public async Task SetAsync(Guid id, ListingDto listing, CancellationToken cancellationToken = default)
    {
        var value = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(listing));
        await ExecuteAsync($"set {Key(id)} 0 30 {value.Length}\r\n{value}\r\n", cancellationToken);
    }

    public async Task InvalidateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _ = await ExecuteAsync($"delete {Key(id)}\r\n", cancellationToken);
}

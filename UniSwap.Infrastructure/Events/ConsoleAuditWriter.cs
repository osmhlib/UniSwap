using UniSwap.Application.Interfaces;

namespace UniSwap.Infrastructure.Events;

public sealed class ConsoleAuditWriter : IAuditWriter
{
    public Task WriteAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}

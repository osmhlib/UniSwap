namespace UniSwap.Application.Interfaces;

public interface IAuditWriter
{
    Task WriteAsync(string message, CancellationToken cancellationToken = default);
}

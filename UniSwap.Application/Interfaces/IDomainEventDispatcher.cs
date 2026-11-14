using UniSwap.Domain.Events;

namespace UniSwap.Application.Interfaces;

/// <summary>
/// Delivers domain facts to subscribers without coupling use cases to them.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

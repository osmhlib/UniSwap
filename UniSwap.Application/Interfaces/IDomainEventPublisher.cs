using UniSwap.Domain.Events;

namespace UniSwap.Application.Interfaces;

public interface IDomainEventPublisher
{
    Task PublishAsync(IHasDomainEvents aggregate, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<IHasDomainEvents> aggregates, CancellationToken cancellationToken = default);
}

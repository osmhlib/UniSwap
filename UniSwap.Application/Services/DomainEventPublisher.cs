using UniSwap.Application.Interfaces;
using UniSwap.Domain.Events;

namespace UniSwap.Application.Services;

public sealed class DomainEventPublisher(IDomainEventDispatcher dispatcher)
{
    public async Task PublishAsync(IHasDomainEvents aggregate, CancellationToken cancellationToken = default)
    {
        var events = aggregate.DomainEvents.ToArray();
        await dispatcher.DispatchAsync(events, cancellationToken);
        aggregate.ClearDomainEvents();
    }

    public async Task PublishAsync(IEnumerable<IHasDomainEvents> aggregates, CancellationToken cancellationToken = default)
    {
        var aggregateArray = aggregates.ToArray();
        var events = aggregateArray.SelectMany(x => x.DomainEvents).ToArray();

        await dispatcher.DispatchAsync(events, cancellationToken);

        foreach (var aggregate in aggregateArray)
            aggregate.ClearDomainEvents();
    }
}

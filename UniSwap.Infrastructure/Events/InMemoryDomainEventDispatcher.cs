using UniSwap.Application.Interfaces;
using UniSwap.Domain.Events;

namespace UniSwap.Infrastructure.Events;

public sealed class InMemoryDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = (System.Collections.IEnumerable?)serviceProvider.GetService(
                typeof(IEnumerable<>).MakeGenericType(handlerType));

            if (handlers is null)
                continue;

            foreach (var handler in handlers)
            {
                var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                var task = (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                await task;
            }
        }
    }
}

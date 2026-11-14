using UniSwap.Application.Interfaces;
using UniSwap.Domain.Events;

namespace UniSwap.Infrastructure.Events;

public sealed class ListingCreatedAuditHandler : IDomainEventHandler<ListingCreated>
{
    public Task HandleAsync(ListingCreated domainEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Audit: listing {domainEvent.ListingId} was created by {domainEvent.OwnerId}.");
        return Task.CompletedTask;
    }
}

public sealed class ListingPublishedAuditHandler : IDomainEventHandler<ListingPublished>
{
    public Task HandleAsync(ListingPublished domainEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Audit: listing {domainEvent.ListingId} was published.");
        return Task.CompletedTask;
    }
}

public sealed class ReservationConfirmedAuditHandler : IDomainEventHandler<ReservationConfirmed>
{
    public Task HandleAsync(ReservationConfirmed domainEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Audit: reservation of listing {domainEvent.ListingId} was confirmed for {domainEvent.RequesterId}.");
        return Task.CompletedTask;
    }
}

public sealed class ExchangeRequestAcceptedAuditHandler : IDomainEventHandler<ExchangeRequestAccepted>
{
    public Task HandleAsync(ExchangeRequestAccepted domainEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Audit: exchange request {domainEvent.ExchangeRequestId} was accepted.");
        return Task.CompletedTask;
    }
}

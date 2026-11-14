using UniSwap.Application.Interfaces;
using UniSwap.Domain.Events;

namespace UniSwap.Infrastructure.Events;

public sealed class ListingCreatedAuditHandler(IAuditWriter auditWriter) : IDomainEventHandler<ListingCreated>
{
    public Task HandleAsync(ListingCreated domainEvent, CancellationToken cancellationToken = default)
    {
        return auditWriter.WriteAsync($"Audit: listing {domainEvent.ListingId} was created by {domainEvent.OwnerId}.", cancellationToken);
    }
}

public sealed class ListingPublishedAuditHandler(IAuditWriter auditWriter) : IDomainEventHandler<ListingPublished>
{
    public Task HandleAsync(ListingPublished domainEvent, CancellationToken cancellationToken = default)
    {
        return auditWriter.WriteAsync($"Audit: listing {domainEvent.ListingId} was published.", cancellationToken);
    }
}

public sealed class ReservationConfirmedAuditHandler(IAuditWriter auditWriter) : IDomainEventHandler<ReservationConfirmed>
{
    public Task HandleAsync(ReservationConfirmed domainEvent, CancellationToken cancellationToken = default)
    {
        return auditWriter.WriteAsync($"Audit: reservation of listing {domainEvent.ListingId} was confirmed for {domainEvent.RequesterId}.", cancellationToken);
    }
}

public sealed class ExchangeRequestAcceptedAuditHandler(IAuditWriter auditWriter) : IDomainEventHandler<ExchangeRequestAccepted>
{
    public Task HandleAsync(ExchangeRequestAccepted domainEvent, CancellationToken cancellationToken = default)
    {
        return auditWriter.WriteAsync($"Audit: exchange request {domainEvent.ExchangeRequestId} was accepted.", cancellationToken);
    }
}

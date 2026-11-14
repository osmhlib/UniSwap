using Microsoft.Extensions.DependencyInjection;
using UniSwap.Application.Interfaces;
using UniSwap.Application.Services;
using UniSwap.Application.UseCases.Exchanges;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Domain.Enums;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Events;
using UniSwap.Infrastructure.Events;
using UniSwap.Infrastructure.Repositories;

var services = new ServiceCollection();
services.AddSingleton<InMemoryStore>();
services.AddScoped<InMemoryUnitOfWork>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InMemoryUnitOfWork>());
services.AddScoped<IListingRepository, InMemoryListingRepository>();
services.AddScoped<IExchangeRequestRepository, InMemoryExchangeRequestRepository>();
services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
services.AddSingleton<RecordingAuditWriter>();
services.AddSingleton<IAuditWriter>(sp => sp.GetRequiredService<RecordingAuditWriter>());
services.AddScoped<IDomainEventHandler<ListingCreated>, ListingCreatedAuditHandler>();
services.AddScoped<IDomainEventHandler<ListingPublished>, ListingPublishedAuditHandler>();
services.AddScoped<IDomainEventHandler<ReservationConfirmed>, ReservationConfirmedAuditHandler>();
services.AddScoped<IDomainEventHandler<ExchangeRequestAccepted>, ExchangeRequestAcceptedAuditHandler>();
services.AddScoped<CreateListing>();
services.AddScoped<PublishListing>();
services.AddScoped<SendExchangeRequest>();
services.AddScoped<AcceptExchangeRequest>();

using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;
var listingId = await sp.GetRequiredService<CreateListing>().ExecuteAsync(Guid.NewGuid(), "Book", "Description");
await sp.GetRequiredService<PublishListing>().ExecuteAsync(listingId);
var requestId = await sp.GetRequiredService<SendExchangeRequest>().ExecuteAsync(listingId, Guid.NewGuid());
await sp.GetRequiredService<AcceptExchangeRequest>().ExecuteAsync(requestId);

var listing = await sp.GetRequiredService<IListingRepository>().GetByIdAsync(listingId);
var request = await sp.GetRequiredService<IExchangeRequestRepository>().GetByIdAsync(requestId);
var messages = sp.GetRequiredService<RecordingAuditWriter>().Messages;
if (listing?.Status != ListingStatus.Reserved || request?.Status != ExchangeStatus.Accepted ||
    messages.Count != 4 || !messages[0].Contains("was created") ||
    !messages[1].Contains("was published") || !messages[2].Contains("was accepted") ||
    !messages[3].Contains("was confirmed"))
    throw new InvalidOperationException("Exchange workflow or audit events failed.");

Console.WriteLine("Exchange workflow and all four audit handlers passed.");

var uncommitted = new Listing(Guid.NewGuid(), "Uncommitted", "Test");
using (var pendingScope = provider.CreateScope())
{
    var repository = pendingScope.ServiceProvider.GetRequiredService<IListingRepository>();
    await repository.AddAsync(uncommitted);
    using var otherScope = provider.CreateScope();
    if (await otherScope.ServiceProvider.GetRequiredService<IListingRepository>().GetByIdAsync(uncommitted.Id) is not null)
        throw new InvalidOperationException("Uncommitted changes leaked to another scope.");
}
using (var checkScope = provider.CreateScope())
{
    if (await checkScope.ServiceProvider.GetRequiredService<IListingRepository>().GetByIdAsync(uncommitted.Id) is not null)
        throw new InvalidOperationException("Discarded changes were persisted.");
    var savedListing = await checkScope.ServiceProvider.GetRequiredService<IListingRepository>().GetByIdAsync(listingId);
    var savedRequest = await checkScope.ServiceProvider.GetRequiredService<IExchangeRequestRepository>().GetByIdAsync(requestId);
    if (savedListing?.Status != ListingStatus.Reserved || savedRequest?.Status != ExchangeStatus.Accepted)
        throw new InvalidOperationException("Committed changes are not visible in a new scope.");
}
using (var canceledScope = provider.CreateScope())
{
    var repo = canceledScope.ServiceProvider.GetRequiredService<IListingRepository>();
    var listingToCancel = new Listing(Guid.NewGuid(), "Canceled", "Test");
    await repo.AddAsync(listingToCancel);
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    try
    {
        await canceledScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cts.Token);
        throw new InvalidOperationException("Canceled save succeeded.");
    }
    catch (OperationCanceledException) { }
    using var checkScope = provider.CreateScope();
    if (await checkScope.ServiceProvider.GetRequiredService<IListingRepository>().GetByIdAsync(listingToCancel.Id) is not null)
        throw new InvalidOperationException("Canceled changes were persisted.");
}
Console.WriteLine("Unit of Work isolation, commit and cancellation passed.");

sealed class RecordingAuditWriter : IAuditWriter
{
    public List<string> Messages { get; } = [];
    public Task WriteAsync(string message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}

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
var exchangeOwner = Guid.NewGuid();
var exchangeRequester = Guid.NewGuid();
var listingId = await sp.GetRequiredService<CreateListing>().ExecuteAsync(exchangeOwner, "Book", "Description", "Books", "Used");
var offeredListingId = await sp.GetRequiredService<CreateListing>().ExecuteAsync(exchangeRequester, "Other book", "Description", "Books", "Used");
await sp.GetRequiredService<PublishListing>().ExecuteAsync(listingId);
await sp.GetRequiredService<PublishListing>().ExecuteAsync(offeredListingId);
var requestId = await sp.GetRequiredService<SendExchangeRequest>().ExecuteAsync(listingId, offeredListingId, exchangeRequester);
await sp.GetRequiredService<AcceptExchangeRequest>().ExecuteAsync(requestId, exchangeOwner);

var listing = await sp.GetRequiredService<IListingRepository>().GetByIdAsync(listingId);
var request = await sp.GetRequiredService<IExchangeRequestRepository>().GetByIdAsync(requestId);
var messages = sp.GetRequiredService<RecordingAuditWriter>().Messages;
if (listing?.Status != ListingStatus.InExchange || request?.Status != ExchangeStatus.Accepted ||
    messages.Count != 5 || !messages[0].Contains("was created") ||
    !messages[1].Contains("was created") || !messages[2].Contains("was published") ||
    !messages[3].Contains("was published") || !messages[4].Contains("was accepted"))
    throw new InvalidOperationException("Exchange workflow or audit events failed.");

Console.WriteLine("Exchange workflow and audit handlers passed.");

var uncommitted = new Listing(Guid.NewGuid(), "Uncommitted", "Test", "Books", "Used");
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
    if (savedListing?.Status != ListingStatus.InExchange || savedRequest?.Status != ExchangeStatus.Accepted)
        throw new InvalidOperationException("Committed changes are not visible in a new scope.");
}
using (var canceledScope = provider.CreateScope())
{
    var repo = canceledScope.ServiceProvider.GetRequiredService<IListingRepository>();
    var listingToCancel = new Listing(Guid.NewGuid(), "Canceled", "Test", "Books", "Used");
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

// Domain rules must reject invalid transitions without changing either aggregate.
var owner = Guid.NewGuid();
var requester = Guid.NewGuid();
var reservedListing = new Listing(owner, "Textbook", "A useful book", "Books", "Used");
reservedListing.Publish();
var reservation = new Reservation(reservedListing, requester);
var competingReservation = new Reservation(reservedListing, Guid.NewGuid());
Expect<UnauthorizedAccessException>(() => reservation.Accept(requester, reservedListing));
if (reservedListing.Status != ListingStatus.Published || reservation.Status != ReservationStatus.Pending)
    throw new InvalidOperationException("Unauthorized acceptance changed state.");
reservation.Accept(owner, reservedListing);
Expect<InvalidOperationException>(() => competingReservation.Accept(owner, reservedListing));
Expect<InvalidOperationException>(() => reservation.Cancel(requester));
Expect<InvalidOperationException>(() => reservedListing.Update("New title", "Description", "Books", "Used"));
if (reservedListing.Status != ListingStatus.Reserved || competingReservation.Status != ReservationStatus.Pending)
    throw new InvalidOperationException("Reservation state is inconsistent.");

var moderated = new Listing(owner, "Another book", "Description", "Books", "Used");
moderated.Publish();
Expect<ArgumentException>(() => moderated.Hide(" "));
moderated.Hide("Inappropriate content");
if (moderated.Status != ListingStatus.Hidden || moderated.ModerationReason != "Inappropriate content")
    throw new InvalidOperationException("Moderation state is inconsistent.");
moderated.ReturnToDraft();
moderated.Publish();
moderated.Reject("Invalid content");
Expect<InvalidOperationException>(() => moderated.StartExchange());
Expect<ArgumentException>(() => new Listing(owner, "ab", "Description", "Books", "Used"));
Expect<ArgumentException>(() => new Listing(owner, "Book", new string('x', 2001), "Books", "Used"));
var target = new Listing(owner, "Target", "Description", "Books", "Used");
var offeredItem = new Listing(requester, "Offered", "Description", "Books", "Used");
target.Publish(); offeredItem.Publish();
var offer = new ExchangeRequest(target, offeredItem, requester);
Expect<UnauthorizedAccessException>(() => offer.Accept(requester, target, offeredItem));
if (target.Status != ListingStatus.Published || offeredItem.Status != ListingStatus.Published)
    throw new InvalidOperationException("Unauthorized exchange changed state.");
offer.Accept(owner, target, offeredItem);
Expect<UnauthorizedAccessException>(() => offer.Complete(Guid.NewGuid(), target, offeredItem));
offer.Complete(requester, target, offeredItem);
if (target.Status != ListingStatus.Completed || offeredItem.Status != ListingStatus.Completed || offer.Status != ExchangeStatus.Completed)
    throw new InvalidOperationException("Exchange completion did not update both listings.");
Console.WriteLine("Listing, reservation, exchange, access and moderation domain rules passed.");

static void Expect<TException>(Action action) where TException : Exception
{
    try { action(); }
    catch (TException) { return; }
    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

sealed class RecordingAuditWriter : IAuditWriter
{
    public List<string> Messages { get; } = [];
    public Task WriteAsync(string message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}

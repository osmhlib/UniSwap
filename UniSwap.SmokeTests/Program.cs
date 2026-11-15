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
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using UniSwap.WebApi.Dtos;
using UniSwap.WebApi.Validation;
using UniSwap.Infrastructure.Caching;
using UniSwap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

var services = new ServiceCollection();
services.AddSingleton<InMemoryStore>();
services.AddScoped<InMemoryUnitOfWork>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InMemoryUnitOfWork>());
services.AddScoped<IListingRepository, InMemoryListingRepository>();
services.AddScoped<IExchangeRequestRepository, InMemoryExchangeRequestRepository>();
services.AddSingleton<IListingCache, TestListingCache>();
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
services.AddScoped<GetListing>();
services.AddScoped<SendExchangeRequest>();
services.AddScoped<AcceptExchangeRequest>();

using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;
var exchangeOwner = Guid.NewGuid();
var exchangeRequester = Guid.NewGuid();
var listingId = await sp.GetRequiredService<CreateListing>().ExecuteAsync(exchangeOwner, "Book", "Description", "Books", "Used");
var getListing = sp.GetRequiredService<GetListing>();
if (await getListing.ExecuteAsync(listingId) is not null || await getListing.ExecuteAsync(Guid.NewGuid()) is not null)
    throw new InvalidOperationException("Draft or missing listing was exposed.");
var offeredListingId = await sp.GetRequiredService<CreateListing>().ExecuteAsync(exchangeRequester, "Other book", "Description", "Books", "Used");
await sp.GetRequiredService<PublishListing>().ExecuteAsync(listingId, exchangeOwner);
var listingDto = await getListing.ExecuteAsync(listingId);
if (listingDto is null || listingDto.Id != listingId || listingDto.OwnerId != exchangeOwner ||
    listingDto.Title != "Book" || listingDto.Description != "Description" || listingDto.Category != "Books" ||
    listingDto.Condition != "Used" || listingDto.Status != "Published" || listingDto.PublishedAtUtc is null)
    throw new InvalidOperationException("Listing DTO mapping failed.");
var offlineReader = new GetListing(sp.GetRequiredService<IListingRepository>(), new MemcachedListingCache("127.0.0.1", 1));
if ((await offlineReader.ExecuteAsync(listingId))?.Id != listingId)
    throw new InvalidOperationException("Unavailable cache did not fall back to the repository.");
var testCache = (TestListingCache)sp.GetRequiredService<IListingCache>();
if (testCache.Values.Count != 1 || await getListing.ExecuteAsync(listingId) != listingDto)
    throw new InvalidOperationException("Listing cache did not serve the cached DTO.");
await sp.GetRequiredService<PublishListing>().ExecuteAsync(offeredListingId, exchangeRequester);
var requestId = await sp.GetRequiredService<SendExchangeRequest>().ExecuteAsync(listingId, offeredListingId, exchangeRequester);
await sp.GetRequiredService<AcceptExchangeRequest>().ExecuteAsync(requestId, exchangeOwner);
if (testCache.Values.ContainsKey(listingId) || (await getListing.ExecuteAsync(listingId))?.Status != "InExchange")
    throw new InvalidOperationException("Exchange did not invalidate the listing cache.");

var listing = await sp.GetRequiredService<IListingRepository>().GetByIdAsync(listingId);
var request = await sp.GetRequiredService<IExchangeRequestRepository>().GetByIdAsync(requestId);
var messages = sp.GetRequiredService<RecordingAuditWriter>().Messages;
if (listing?.Status != ListingStatus.InExchange || request?.Status != ExchangeStatus.Accepted ||
    messages.Count != 5 || !messages[0].Contains("was created") ||
    !messages[1].Contains("was created") || !messages[2].Contains("was published") ||
    !messages[3].Contains("was published") || !messages[4].Contains("was accepted"))
    throw new InvalidOperationException("Exchange workflow or audit events failed.");

Console.WriteLine("Exchange workflow and audit handlers passed.");

var hiddenListingId = await sp.GetRequiredService<CreateListing>()
    .ExecuteAsync(exchangeOwner, "Cache visibility", "Description", "Books", "Used");
await sp.GetRequiredService<PublishListing>().ExecuteAsync(hiddenListingId, exchangeOwner);
await getListing.ExecuteAsync(hiddenListingId);
var hiddenListing = (await sp.GetRequiredService<IListingRepository>().GetByIdAsync(hiddenListingId))!;
hiddenListing.Hide("A moderation decision");
await sp.GetRequiredService<IListingRepository>().UpdateAsync(hiddenListing);
await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
if (!testCache.Values.ContainsKey(hiddenListingId) || await getListing.ExecuteAsync(hiddenListingId) is not null)
    throw new InvalidOperationException("A stale cache entry exposed a hidden listing.");
Console.WriteLine("Stale listing cache cannot bypass publication status.");

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

var validationServices = new ServiceCollection();
validationServices.AddScoped<IValidator<CreateListingRequest>, CreateListingRequestValidator>();
validationServices.AddScoped<IValidator<SendExchangeRequestDto>, SendExchangeRequestValidator>();
using var validationProvider = validationServices.BuildServiceProvider();
var filter = new ValidationFilter(validationProvider);
async Task<(ActionExecutingContext Context, bool Executed)> CheckFilter(object requestDto)
{
    var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    var context = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { ["request"] = requestDto }, new object());
    var executed = false;
    await filter.OnActionExecutionAsync(context, () =>
    {
        executed = true;
        return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
    });
    return (context, executed);
}
var invalid = await CheckFilter(new CreateListingRequest(" x ", " ", "", ""));
if (invalid.Executed || invalid.Context.Result is not BadRequestObjectResult { Value: ValidationProblemDetails problem } ||
    !problem.Errors.ContainsKey("Title") || !problem.Errors.ContainsKey("Description"))
    throw new InvalidOperationException("Validation filter did not return structured errors.");
var valid = await CheckFilter(new CreateListingRequest("Book", "Description", "Books", "Used"));
if (!valid.Executed || valid.Context.Result is not null)
    throw new InvalidOperationException("Valid request did not pass the filter.");
var sameId = Guid.NewGuid();
var semantic = await CheckFilter(new SendExchangeRequestDto(sameId, sameId));
if (semantic.Executed || semantic.Context.Result is not BadRequestObjectResult { Value: ValidationProblemDetails semanticProblem } ||
    !semanticProblem.Errors.ContainsKey("OfferedListingId"))
    throw new InvalidOperationException("Semantic validation failed.");
Console.WriteLine("Validation filter, structured errors and semantic rule passed.");

// Validate relational mappings and migration generation without requiring a running database.
var options = new DbContextOptionsBuilder<UniSwapDbContext>()
    .UseNpgsql("Host=localhost;Database=unisw ap")
    .Options;
await using (var db = new UniSwapDbContext(options))
{
    foreach (var type in new[] { typeof(User), typeof(Listing), typeof(ListingImage), typeof(Reservation), typeof(ExchangeRequest) })
        if (db.Model.FindEntityType(type) is null) throw new InvalidOperationException($"Missing mapping: {type.Name}.");
    foreach (var type in new[] { typeof(Listing), typeof(Reservation), typeof(ExchangeRequest) })
    {
        var version = db.Model.FindEntityType(type)!.FindProperty("Version");
        if (version is null || !version.IsConcurrencyToken) throw new InvalidOperationException($"Missing concurrency token: {type.Name}.");
    }
    var migrationSql = db.GetService<IMigrator>().GenerateScript();
    if (!migrationSql.Contains("CREATE TABLE \"ListingImages\"") || !migrationSql.Contains("CREATE TABLE \"Reservations\""))
        throw new InvalidOperationException("Migration SQL is missing expected tables.");
}
Expect<ArgumentException>(() => new ListingImage(Guid.NewGuid(), "key", "image/gif", 100));
Expect<ArgumentOutOfRangeException>(() => new ListingImage(Guid.NewGuid(), "key", "image/png", 5 * 1024 * 1024 + 1));
Console.WriteLine("PostgreSQL model, migration SQL and image metadata validation passed.");

await ImageStorageSmokeTests.RunAsync();

if (Environment.GetEnvironmentVariable("UniSwapTests__Postgres") is { Length: > 0 } testDatabase)
    await PostgresSmokeTests.RunAsync(testDatabase);

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

sealed class TestListingCache : IListingCache
{
    public Dictionary<Guid, UniSwap.Application.Dtos.ListingDto> Values { get; } = [];
    public Task<UniSwap.Application.Dtos.ListingDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Values.GetValueOrDefault(id));
    public Task SetAsync(Guid id, UniSwap.Application.Dtos.ListingDto listing, CancellationToken cancellationToken = default)
    {
        Values[id] = listing;
        return Task.CompletedTask;
    }
    public Task InvalidateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Values.Remove(id);
        return Task.CompletedTask;
    }
}

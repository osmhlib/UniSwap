using UniSwap.Application.UseCases.Exchanges;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Infrastructure.Repositories;
using UniSwap.Application.Services;
using UniSwap.Application.Interfaces;
using UniSwap.Infrastructure.Events;
using UniSwap.Domain.Events;
using FluentValidation;
using UniSwap.WebApi.Dtos;
using UniSwap.WebApi.Validation;
using UniSwap.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddScoped<IValidator<CreateListingRequest>, CreateListingRequestValidator>();
builder.Services.AddScoped<IValidator<SendExchangeRequestDto>, SendExchangeRequestValidator>();
builder.Services.AddControllers(options => options.Filters.AddService<ValidationFilter>());

builder.Services.AddOpenApi();

builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<IListingCache>(_ => new MemcachedListingCache(
    builder.Configuration["Memcached:Host"] ?? "localhost",
    builder.Configuration.GetValue<int?>("Memcached:Port") ?? 11211));
builder.Services.AddScoped<InMemoryUnitOfWork>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InMemoryUnitOfWork>());
builder.Services.AddScoped<IListingRepository, InMemoryListingRepository>();
builder.Services.AddScoped<IExchangeRequestRepository, InMemoryExchangeRequestRepository>();
builder.Services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
builder.Services.AddSingleton<IAuditWriter, ConsoleAuditWriter>();

builder.Services.AddScoped<IDomainEventHandler<ListingCreated>, ListingCreatedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ListingPublished>, ListingPublishedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ReservationConfirmed>, ReservationConfirmedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ExchangeRequestAccepted>, ExchangeRequestAcceptedAuditHandler>();

builder.Services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
builder.Services.AddScoped<CreateListing>();
builder.Services.AddScoped<PublishListing>();
builder.Services.AddScoped<GetListing>();
builder.Services.AddScoped<SendExchangeRequest>();
builder.Services.AddScoped<AcceptExchangeRequest>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

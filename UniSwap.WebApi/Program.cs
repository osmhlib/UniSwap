using UniSwap.Application.Moderation;
using UniSwap.Application.Reservations;
using UniSwap.WebApi.Controllers;
using UniSwap.Application.Listings;
using UniSwap.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using UniSwap.Application.Users;
using UniSwap.Domain.Entities;
using UniSwap.WebApi;
using UniSwap.WebApi.Auth;
using UniSwap.Application.UseCases.Exchanges;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<IValidator<UpdateListingRequest>, UpdateListingRequestValidator>();
builder.Services.AddScoped<IValidator<ListingPagingRequest>, ListingPagingValidator>();
builder.Services.AddScoped<IValidator<BrowseListingsRequest>, BrowseListingsValidator>();
builder.Services.AddControllers(options => options.Filters.AddService<ValidationFilter>());

builder.Services.AddOpenApi(BearerOpenApi.Configure);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiExceptionHandler.AddDefaultCode);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAuthentication(AccountIdentity.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(AccountIdentity.Scheme, _ => { });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AccountIdentity.AdminPolicy, policy => policy.RequireRole(nameof(UserRole.Admin)));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAccountPasswords, AccountPasswordHasher>();
builder.Services.AddScoped<EfAccounts>();
builder.Services.AddScoped<IAccountRepository>(sp => sp.GetRequiredService<EfAccounts>());
builder.Services.AddScoped<IAccountSessions>(sp => sp.GetRequiredService<EfAccounts>());
builder.Services.AddScoped<Accounts>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();

var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddDbContext<UniSwapDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("UniSwap")
    ?? throw new InvalidOperationException("Connection string UniSwap is not configured.")));
builder.Services.AddSingleton<IListingCache>(_ => new MemcachedListingCache(
    builder.Configuration["Memcached:Host"] ?? "localhost",
    builder.Configuration.GetValue<int?>("Memcached:Port") ?? 11211));
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UniSwapDbContext>());
builder.Services.AddScoped<IListingRepository, EfListingRepository>();
builder.Services.AddScoped<EfExchangeRequestRepository>();
builder.Services.AddScoped<IExchangeRequestRepository>(sp => sp.GetRequiredService<EfExchangeRequestRepository>());
builder.Services.AddScoped<IExchangeCatalog>(sp => sp.GetRequiredService<EfExchangeRequestRepository>());
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
builder.Services.AddScoped<ReservationWorkflow>();
builder.Services.AddScoped<IReservationRepository, EfReservations>();
builder.Services.AddScoped<IValidator<CreateReservationRequest>, CreateReservationValidator>();
builder.Services.AddScoped<ListingManagement>();
builder.Services.AddScoped<IListingCatalog, EfListingCatalog>();
builder.Services.AddScoped<IListingImageRepository, EfListingImages>();
builder.Services.AddScoped<ListingImages>();
builder.Services.AddSingleton<IListingImageStorage>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var root = Path.GetFullPath(config["ListingImages:Path"] ?? "App_Data/listing-images", environment.ContentRootPath);
    var logger = sp.GetRequiredService<ILogger<LocalListingImageStorage>>();
    return new LocalListingImageStorage(root, ex => logger.LogWarning(ex, "Could not remove a listing image file."));
});
builder.Services.AddScoped<SendExchangeRequest>();
builder.Services.AddScoped<AcceptExchangeRequest>();
builder.Services.AddScoped<ManageExchange>();
builder.Services.AddScoped<ModerateListing>();
builder.Services.AddScoped<IModerationCatalog, EfModerationCatalog>();
builder.Services.AddScoped<IValidator<ModerationReasonRequest>, ModerationReasonValidator>();
builder.Services.AddScoped<IValidator<ModerationQueueRequest>, ModerationQueueValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = context => context.HttpContext.Response.StatusCode < 500
});
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();

app.UseAuthorization();
app.UseMiddleware<UploadSizeLimitMiddleware>();

app.MapControllers();

await AdminBootstrap.RunAsync(app);
await app.RunAsync();

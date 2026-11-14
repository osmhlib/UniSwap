using UniSwap.Application.Interfaces;
using UniSwap.Application.UseCases.Exchanges;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IListingRepository, InMemoryListingRepository>();
builder.Services.AddSingleton<IExchangeRequestRepository, InMemoryExchangeRequestRepository>();

builder.Services.AddScoped<CreateListing>();
builder.Services.AddScoped<SendExchangeRequest>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
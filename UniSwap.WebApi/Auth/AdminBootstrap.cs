using FluentValidation;
using UniSwap.Application.Users;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Auth;

public static class AdminBootstrap
{
    public static async Task RunAsync(WebApplication app)
    {
        var section = app.Configuration.GetSection("AdminBootstrap");
        var email = section["Email"];
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrEmpty(password)) return;

        var request = new RegisterRequest(section["Name"] ?? "Administrator", email ?? "", password ?? "");
        await using var scope = app.Services.CreateAsyncScope();
        var validation = await scope.ServiceProvider.GetRequiredService<IValidator<RegisterRequest>>()
            .ValidateAsync(request, app.Lifetime.ApplicationStopping);
        if (!validation.IsValid)
            throw new InvalidOperationException("AdminBootstrap settings are incomplete or invalid.");

        await scope.ServiceProvider.GetRequiredService<Accounts>()
            .EnsureAdminAsync(request.Name, request.Email, request.Password, app.Lifetime.ApplicationStopping);
    }
}

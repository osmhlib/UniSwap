using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace UniSwap.WebApi.Auth;

public static class BearerOpenApi
{
    public static void Configure(OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, _) =>
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any())
                return Task.CompletedTask;

            var document = context.Document!;
            if (document.Components?.SecuritySchemes?.ContainsKey(AccountIdentity.Scheme) != true)
                document.AddComponent(AccountIdentity.Scheme, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Description = "Access token returned by POST /api/auth/login."
                });

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(AccountIdentity.Scheme, document)] = []
            });
            return Task.CompletedTask;
        });
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ScimSlim.Abstractions;
using ScimSlim.Authentication;
using ScimSlim.Endpoints;

namespace ScimSlim.Extensions;

/// <summary>
/// Registration and routing helpers for the SCIM server middleware.
/// </summary>
public static class ScimServiceExtensions
{
    /// <summary>
    /// Registers the SCIM stores, options, the static-token authentication scheme
    /// and a matching <c>ScimToken</c> authorization policy.
    /// </summary>
    public static IServiceCollection AddScim<TUserStore, TGroupStore>(
        this IServiceCollection services,
        Action<ScimOptions>? configure = null)
        where TUserStore : class, IScimUserStore
        where TGroupStore : class, IScimGroupStore
    {
        var options = new ScimOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddScoped<IScimUserStore, TUserStore>();
        services.AddScoped<IScimGroupStore, TGroupStore>();

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ScimTokenAuthenticationHandler>(
                ScimAuthenticationDefaults.Scheme, _ => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(ScimAuthenticationDefaults.Scheme, policy =>
            {
                policy.AddAuthenticationSchemes(ScimAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
            });

        return services;
    }

    /// <summary>
    /// Maps the SCIM endpoints under <paramref name="prefix"/> (default <c>/v2</c>),
    /// all protected by the <c>ScimToken</c> authorization policy.
    /// </summary>
    public static IEndpointRouteBuilder MapScim(
        this IEndpointRouteBuilder app,
        string prefix = "/v2")
    {
        var scim = app.MapGroup(prefix)
            .RequireAuthorization(ScimAuthenticationDefaults.Scheme);

        scim.MapGet("/Users", UsersEndpoint.List);
        scim.MapGet("/Users/{id}", UsersEndpoint.Get);
        scim.MapPost("/Users", UsersEndpoint.Create);
        scim.MapPut("/Users/{id}", UsersEndpoint.Replace);
        scim.MapMethods("/Users/{id}", ["PATCH"], UsersEndpoint.Patch);
        scim.MapDelete("/Users/{id}", UsersEndpoint.Delete);

        scim.MapGet("/Groups", GroupsEndpoint.List);
        scim.MapGet("/Groups/{id}", GroupsEndpoint.Get);
        scim.MapPost("/Groups", GroupsEndpoint.Create);
        scim.MapPut("/Groups/{id}", GroupsEndpoint.Replace);
        scim.MapMethods("/Groups/{id}", ["PATCH"], GroupsEndpoint.Patch);
        scim.MapDelete("/Groups/{id}", GroupsEndpoint.Delete);

        scim.MapGet("/ServiceProviderConfig", ScimMetaEndpoint.ServiceProviderConfig);

        return app;
    }
}

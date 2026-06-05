using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScimSlim.Abstractions;

namespace ScimSlim.EntityFramework;

/// <summary>
/// DI helpers for wiring the EF Core-backed SCIM stores.
/// </summary>
public static class ScimEntityFrameworkExtensions
{
    /// <summary>
    /// Registers <see cref="EfScimUserStore"/> and <see cref="EfScimGroupStore"/>
    /// as the SCIM stores, backed by <typeparamref name="TContext"/>.
    /// Call this <em>after</em> <c>AddScim&lt;EfScimUserStore, EfScimGroupStore&gt;()</c>
    /// (or instead register your context and use this as the store provider).
    /// </summary>
    public static IServiceCollection AddScimEntityFrameworkStores<TContext>(
        this IServiceCollection services)
        where TContext : DbContext, IScimDbContext
    {
        services.AddScoped<IScimDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IScimUserStore, EfScimUserStore>();
        services.AddScoped<IScimGroupStore, EfScimGroupStore>();
        return services;
    }
}

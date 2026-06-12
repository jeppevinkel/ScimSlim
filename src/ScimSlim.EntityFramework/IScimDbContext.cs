using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ScimSlim.EntityFramework.Entities;

namespace ScimSlim.EntityFramework;

/// <summary>
/// Implement this on your application's <c>DbContext</c> so the EF-backed SCIM
/// stores can read and write the SCIM entities.
/// </summary>
public interface IScimDbContext
{
    DbSet<ScimUserEntity> ScimUsers { get; }
    DbSet<ScimGroupEntity> ScimGroups { get; }

    ChangeTracker ChangeTracker { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using ScimSlim.Models;

namespace ScimSlim.Abstractions;

/// <summary>
/// Backing store for SCIM User resources. Implement this against your local
/// data store (EF Core, Dapper, etc.). All operations must be idempotent —
/// Authentik performs a full sync periodically.
/// </summary>
public interface IScimUserStore
{
    Task<ScimUser?> GetByIdAsync(string id);

    Task<ScimUser?> GetByExternalIdAsync(string externalId);

    Task<ScimUser?> GetByUsernameAsync(string username);

    Task<(IEnumerable<ScimUser> Users, int Total)> ListAsync(
        string? filter, int startIndex, int count);

    Task<ScimUser> CreateAsync(ScimUser user);

    Task<ScimUser> UpdateAsync(string id, ScimUser user);

    Task ApplyPatchAsync(string id, ScimPatchRequest patch);

    Task DeleteAsync(string id);
}

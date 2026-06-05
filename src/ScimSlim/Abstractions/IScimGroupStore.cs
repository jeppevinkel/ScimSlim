using ScimSlim.Models;

namespace ScimSlim.Abstractions;

/// <summary>
/// Backing store for SCIM Group resources. Implement this against your local
/// data store. All operations must be idempotent.
/// </summary>
public interface IScimGroupStore
{
    Task<ScimGroup?> GetByIdAsync(string id);

    Task<ScimGroup?> GetByExternalIdAsync(string externalId);

    Task<(IEnumerable<ScimGroup> Groups, int Total)> ListAsync(
        string? filter, int startIndex, int count);

    Task<ScimGroup> CreateAsync(ScimGroup group);

    Task<ScimGroup> UpdateAsync(string id, ScimGroup group);

    Task ApplyPatchAsync(string id, ScimPatchRequest patch);

    Task DeleteAsync(string id);
}

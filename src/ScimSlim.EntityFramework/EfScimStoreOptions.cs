namespace ScimSlim.EntityFramework;

/// <summary>
/// Configuration for the EF Core-backed SCIM stores.
/// </summary>
public class EfScimStoreOptions
{
    /// <summary>
    /// Whether deleting a user marks the row as deleted instead of removing it.
    /// Soft-deleted users are excluded from all reads/lists. Defaults to true.
    /// </summary>
    public bool SoftDeleteUsers { get; set; } = true;
}

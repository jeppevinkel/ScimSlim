namespace ScimSlim;

/// <summary>
/// Configuration for the SCIM server middleware.
/// </summary>
public class ScimOptions
{
    /// <summary>
    /// The static Bearer token Authentik must present on every SCIM request.
    /// </summary>
    public string StaticToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether PATCH operations are advertised as supported in
    /// /ServiceProviderConfig. Defaults to true.
    /// </summary>
    public bool SupportPatch { get; set; } = true;

    /// <summary>
    /// Whether filtering is advertised as supported in
    /// /ServiceProviderConfig. Defaults to true.
    /// </summary>
    public bool SupportFiltering { get; set; } = true;

    /// <summary>
    /// Default page size used when a list request omits <c>count</c>.
    /// </summary>
    public int DefaultPageSize { get; set; } = 100;
}

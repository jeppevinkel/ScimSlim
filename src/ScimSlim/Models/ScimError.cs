using System.Text.Json.Serialization;

namespace ScimSlim.Models;

/// <summary>
/// SCIM 2.0 error envelope. Used for all non-success responses.
/// </summary>
public class ScimError
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } =
        ["urn:ietf:params:scim:api:messages:2.0:Error"];

    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// A SCIM detail error keyword (e.g. "uniqueness", "invalidValue").
    /// Optional; only set for the error types SCIM defines it for.
    /// </summary>
    [JsonPropertyName("scimType")]
    public string? ScimType { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

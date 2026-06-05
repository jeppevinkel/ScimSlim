using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScimSlim.Http;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used for SCIM payloads.
/// Property names are declared explicitly with <c>[JsonPropertyName]</c>, so the
/// main concern here is omitting null attributes (SCIM permits leaving them out).
/// </summary>
public static class ScimJson
{
    /// <summary>The SCIM JSON media type.</summary>
    public const string ContentType = "application/scim+json";

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

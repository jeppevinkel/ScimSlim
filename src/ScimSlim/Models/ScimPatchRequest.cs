using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScimSlim.Models;

/// <summary>
/// SCIM 2.0 PatchOp request body (used for PATCH operations).
/// </summary>
public class ScimPatchRequest
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } =
        ["urn:ietf:params:scim:api:messages:2.0:PatchOp"];

    [JsonPropertyName("Operations")]
    public List<ScimPatchOperation> Operations { get; set; } = [];
}

public class ScimPatchOperation
{
    /// <summary>add, remove or replace.</summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("value")]
    public JsonElement? Value { get; set; }
}

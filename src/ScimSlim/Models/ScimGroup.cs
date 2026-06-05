using System.Text.Json.Serialization;

namespace ScimSlim.Models;

/// <summary>
/// SCIM 2.0 core Group resource.
/// </summary>
public class ScimGroup
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } =
        ["urn:ietf:params:scim:schemas:core:2.0:Group"];

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("members")]
    public List<ScimGroupMember>? Members { get; set; }

    [JsonPropertyName("meta")]
    public ScimMeta? Meta { get; set; }
}

public class ScimGroupMember
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

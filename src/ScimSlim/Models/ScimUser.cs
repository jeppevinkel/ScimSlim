using System.Text.Json.Serialization;

namespace ScimSlim.Models;

/// <summary>
/// SCIM 2.0 core User resource. Only the subset of attributes that Authentik's
/// default SCIM property mappings send/expect is modeled.
/// </summary>
public class ScimUser
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } =
        ["urn:ietf:params:scim:schemas:core:2.0:User"];

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;

    [JsonPropertyName("name")]
    public ScimName? Name { get; set; }

    [JsonPropertyName("emails")]
    public List<ScimEmail>? Emails { get; set; }

    [JsonPropertyName("meta")]
    public ScimMeta? Meta { get; set; }
}

public class ScimName
{
    [JsonPropertyName("givenName")]
    public string? GivenName { get; set; }

    [JsonPropertyName("familyName")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }
}

public class ScimEmail
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "work";

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

public class ScimMeta
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTimeOffset LastModified { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

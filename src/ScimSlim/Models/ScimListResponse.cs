using System.Text.Json.Serialization;

namespace ScimSlim.Models;

/// <summary>
/// SCIM 2.0 ListResponse envelope returned by collection (GET) endpoints.
/// </summary>
public class ScimListResponse<T>
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } =
        ["urn:ietf:params:scim:api:messages:2.0:ListResponse"];

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; } = 1;

    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; set; }

    [JsonPropertyName("Resources")]
    public List<T> Resources { get; set; } = [];
}

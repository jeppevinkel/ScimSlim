using System.Text.Json;
using System.Text.RegularExpressions;
using ScimSlim.Models;

namespace ScimSlim.Patching;

/// <summary>
/// Applies SCIM PatchOp operations to in-memory <see cref="ScimUser"/> and
/// <see cref="ScimGroup"/> instances. Covers the operations Authentik emits:
/// attribute replacement on users and member add/remove on groups.
/// Store implementations can call these to avoid hand-rolling patch handling.
/// </summary>
public static class ScimPatch
{
    // Matches a valued path filter such as: members[value eq "abc"]
    private static readonly Regex MemberFilter = new(
        """^members\[\s*value\s+eq\s+"(?<val>[^"]*)"\s*\]$""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Apply(ScimUser user, ScimPatchRequest patch)
    {
        foreach (var op in patch.Operations)
        {
            ApplyToUser(user, op);
        }
    }

    public static void Apply(ScimGroup group, ScimPatchRequest patch)
    {
        foreach (var op in patch.Operations)
        {
            ApplyToGroup(group, op);
        }
    }

    private static void ApplyToUser(ScimUser user, ScimPatchOperation op)
    {
        var isRemove = string.Equals(op.Op, "remove", StringComparison.OrdinalIgnoreCase);

        // No path: the value is an object whose members are the attributes to set.
        if (string.IsNullOrEmpty(op.Path))
        {
            if (op.Value is { ValueKind: JsonValueKind.Object } obj)
            {
                foreach (var prop in obj.EnumerateObject())
                {
                    SetUserAttribute(user, prop.Name, prop.Value, isRemove: false);
                }
            }

            return;
        }

        SetUserAttribute(user, op.Path, op.Value, isRemove);
    }

    private static void SetUserAttribute(
        ScimUser user, string path, JsonElement? value, bool isRemove)
    {
        switch (path.ToLowerInvariant())
        {
            case "active":
                user.Active = !isRemove && (ReadBool(value) ?? user.Active);
                break;
            case "username":
                user.UserName = isRemove ? string.Empty : ReadString(value) ?? user.UserName;
                break;
            case "displayname":
                user.DisplayName = isRemove ? null : ReadString(value);
                break;
            case "externalid":
                user.ExternalId = isRemove ? string.Empty : ReadString(value) ?? user.ExternalId;
                break;
            case "locale":
                user.Locale = isRemove ? null : ReadString(value);
                break;
            case "name.givenname":
                (user.Name ??= new ScimName()).GivenName = isRemove ? null : ReadString(value);
                break;
            case "name.familyname":
                (user.Name ??= new ScimName()).FamilyName = isRemove ? null : ReadString(value);
                break;
            case "name.formatted":
                (user.Name ??= new ScimName()).Formatted = isRemove ? null : ReadString(value);
                break;
            case "name":
                user.Name = isRemove ? null : Deserialize<ScimName>(value);
                break;
            case "emails":
                user.Emails = isRemove ? null : Deserialize<List<ScimEmail>>(value);
                break;
        }
    }

    private static void ApplyToGroup(ScimGroup group, ScimPatchOperation op)
    {
        var opName = op.Op.ToLowerInvariant();
        var path = op.Path ?? string.Empty;

        // members[value eq "id"] — targeted remove of a single member.
        var filterMatch = MemberFilter.Match(path);
        if (filterMatch.Success)
        {
            var id = filterMatch.Groups["val"].Value;
            group.Members?.RemoveAll(m => m.Value == id);
            return;
        }

        if (path.Equals("members", StringComparison.OrdinalIgnoreCase))
        {
            var members = ReadMembers(op.Value);
            switch (opName)
            {
                case "add":
                    group.Members ??= [];
                    foreach (var m in members)
                    {
                        if (group.Members.All(existing => existing.Value != m.Value))
                        {
                            group.Members.Add(m);
                        }
                    }

                    break;
                case "remove":
                    if (members.Count == 0)
                    {
                        group.Members = [];
                    }
                    else
                    {
                        var ids = members.Select(m => m.Value).ToHashSet();
                        group.Members?.RemoveAll(m => ids.Contains(m.Value));
                    }

                    break;
                case "replace":
                    group.Members = members;
                    break;
            }

            return;
        }

        if (path.Equals("displayName", StringComparison.OrdinalIgnoreCase))
        {
            group.DisplayName = opName == "remove"
                ? string.Empty
                : ReadString(op.Value) ?? group.DisplayName;
            return;
        }

        if (path.Equals("externalId", StringComparison.OrdinalIgnoreCase))
        {
            group.ExternalId = opName == "remove"
                ? string.Empty
                : ReadString(op.Value) ?? group.ExternalId;
            return;
        }

        // No path: value object carrying attributes to replace.
        if (string.IsNullOrEmpty(op.Path) && op.Value is { ValueKind: JsonValueKind.Object } obj)
        {
            if (obj.TryGetProperty("displayName", out var dn) && dn.ValueKind == JsonValueKind.String)
            {
                group.DisplayName = dn.GetString() ?? group.DisplayName;
            }

            if (obj.TryGetProperty("members", out var members))
            {
                group.Members = ReadMembers(members);
            }
        }
    }

    private static List<ScimGroupMember> ReadMembers(JsonElement? value)
    {
        if (value is not { } element)
        {
            return [];
        }

        return element.ValueKind switch
        {
            JsonValueKind.Array =>
                element.Deserialize<List<ScimGroupMember>>(Http.ScimJson.Options) ?? [],
            JsonValueKind.Object =>
                element.Deserialize<ScimGroupMember>(Http.ScimJson.Options) is { } single
                    ? [single]
                    : [],
            _ => [],
        };
    }

    private static string? ReadString(JsonElement? value) => value?.ValueKind switch
    {
        JsonValueKind.String => value.Value.GetString(),
        JsonValueKind.Number => value.Value.ToString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null,
    };

    private static bool? ReadBool(JsonElement? value) => value?.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String when bool.TryParse(value.Value.GetString(), out var b) => b,
        _ => null,
    };

    private static T? Deserialize<T>(JsonElement? value) =>
        value is { } element ? element.Deserialize<T>(Http.ScimJson.Options) : default;
}

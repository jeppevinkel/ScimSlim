using ScimSlim.EntityFramework.Entities;
using ScimSlim.Models;

namespace ScimSlim.EntityFramework;

/// <summary>
/// Maps between EF entities and SCIM resource models.
/// </summary>
internal static class ScimEntityMapping
{
    public static ScimUser ToScim(this ScimUserEntity e) => new()
    {
        Id = e.Id,
        ExternalId = e.ExternalId,
        UserName = e.UserName,
        DisplayName = e.DisplayName,
        Locale = e.Locale,
        Active = e.Active,
        Name = e is { GivenName: null, FamilyName: null, Formatted: null }
            ? null
            : new ScimName
            {
                GivenName = e.GivenName,
                FamilyName = e.FamilyName,
                Formatted = e.Formatted,
            },
        Emails = e.Emails.Count == 0
            ? null
            : e.Emails.Select(m => new ScimEmail
            {
                Value = m.Value,
                Type = m.Type,
                Primary = m.Primary,
            }).ToList(),
        Meta = new ScimMeta
        {
            ResourceType = "User",
            Created = e.Created,
            LastModified = e.LastModified,
        },
    };

    /// <summary>Copies SCIM user values onto an entity (preserves <c>Id</c>/timestamps).
    /// Emails are diffed by <c>Value</c> so unchanged mail addresses are left untouched.</summary>
    public static void ApplyFrom(this ScimUserEntity e, ScimUser user)
    {
        e.ExternalId = user.ExternalId;
        e.UserName = user.UserName;
        e.DisplayName = user.DisplayName;
        e.Locale = user.Locale;
        e.Active = user.Active;
        e.GivenName = user.Name?.GivenName;
        e.FamilyName = user.Name?.FamilyName;
        e.Formatted = user.Name?.Formatted;
        
        var incoming = (user.Emails ?? [])
            .Where(m => !string.IsNullOrEmpty(m.Value))
            .DistinctBy(m => m.Value)
            .ToDictionary(m => m.Value);

        // 1. Remove emails no longer present (and dedupe any pre-existing duplicates).
        var seen = new HashSet<string>();
        e.Emails.RemoveAll(m => !incoming.ContainsKey(m.Value) || !seen.Add(m.Value));
        
        // 2. Update survivors in place; strip them from the incoming set.
        foreach (var email in e.Emails)
        {
            var inc = incoming[email.Value];
            email.Type = inc.Type;
            email.Primary = inc.Primary;
            incoming.Remove(email.Value);
        }
        
        // 3. Whatever remains is genuinely new.
        foreach (var inc in incoming.Values)
        {
            e.Emails.Add(new ScimEmailEntity()
            {
                UserId = e.Id,
                Value = inc.Value,
                Type = inc.Type,
                Primary = inc.Primary,
            });
        }
    }

    public static ScimGroup ToScim(this ScimGroupEntity e) => new()
    {
        Id = e.Id,
        ExternalId = e.ExternalId,
        DisplayName = e.DisplayName,
        Members = e.Members.Count == 0
            ? null
            : e.Members.Select(m => new ScimGroupMember
            {
                Value = m.Value,
                Display = m.Display,
            }).ToList(),
        Meta = new ScimMeta
        {
            ResourceType = "Group",
            Created = e.Created,
            LastModified = e.LastModified,
        },
    };

    /// <summary>Copies SCIM group values onto an entity (preserves <c>Id</c>/timestamps).
    /// Members are diffed by <c>Value</c> so unchanged memberships are left untouched.</summary>
    public static void ApplyFrom(this ScimGroupEntity e, ScimGroup group)
    {
        e.ExternalId = group.ExternalId;
        e.DisplayName = group.DisplayName;
        
        var incoming = (group.Members ?? [])
            .Where(m => !string.IsNullOrEmpty(m.Value))
            .DistinctBy(m => m.Value)
            .ToDictionary(m => m.Value);

        // 1. Remove members no longer present (and dedupe any pre-existing duplicates).
        var seen = new HashSet<string>();
        e.Members.RemoveAll(m => !incoming.ContainsKey(m.Value) || !seen.Add(m.Value));
        
        // 2. Update survivors in place; strip them from the incoming set.
        foreach (var member in e.Members)
        {
            var inc = incoming[member.Value];
            member.Display = inc.Display;
            incoming.Remove(member.Value);
        }
        
        // 3. Whatever remains is genuinely new.
        foreach (var inc in incoming.Values)
        {
            e.Members.Add(new ScimGroupMemberEntity
            {
                GroupId = e.Id,
                Value = inc.Value,
                Display = inc.Display,
            });
        }
    }
}

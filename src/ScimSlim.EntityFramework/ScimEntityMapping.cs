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

    /// <summary>Copies SCIM user values onto an entity (preserves <c>Id</c>/timestamps).</summary>
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

        e.Emails.Clear();
        if (user.Emails is not null)
        {
            foreach (var email in user.Emails)
            {
                e.Emails.Add(new ScimEmailEntity
                {
                    UserId = e.Id,
                    Value = email.Value,
                    Type = email.Type,
                    Primary = email.Primary,
                });
            }
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

    /// <summary>Copies SCIM group values onto an entity (preserves <c>Id</c>/timestamps).</summary>
    public static void ApplyFrom(this ScimGroupEntity e, ScimGroup group)
    {
        e.ExternalId = group.ExternalId;
        e.DisplayName = group.DisplayName;

        e.Members.Clear();
        if (group.Members is not null)
        {
            foreach (var member in group.Members)
            {
                e.Members.Add(new ScimGroupMemberEntity
                {
                    GroupId = e.Id,
                    Value = member.Value,
                    Display = member.Display,
                });
            }
        }
    }
}

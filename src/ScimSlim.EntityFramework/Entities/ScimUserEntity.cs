namespace ScimSlim.EntityFramework.Entities;

/// <summary>
/// EF Core entity backing a SCIM User. Add it (and <see cref="ScimEmailEntity"/>)
/// to your <c>DbContext</c> via <see cref="ScimModelBuilderExtensions.ApplyScimModel"/>.
/// </summary>
public class ScimUserEntity
{
    public string Id { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Locale { get; set; }
    public bool Active { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Formatted { get; set; }

    public List<ScimEmailEntity> Emails { get; set; } = [];

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

public class ScimEmailEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = "work";
    public bool Primary { get; set; }
}

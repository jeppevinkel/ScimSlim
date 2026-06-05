namespace ScimSlim.EntityFramework.Entities;

/// <summary>
/// EF Core entity backing a SCIM Group.
/// </summary>
public class ScimGroupEntity
{
    public string Id { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public List<ScimGroupMemberEntity> Members { get; set; } = [];

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

public class ScimGroupMemberEntity
{
    public int Id { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Display { get; set; }
}

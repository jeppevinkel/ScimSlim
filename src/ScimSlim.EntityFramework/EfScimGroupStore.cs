using Microsoft.EntityFrameworkCore;
using ScimSlim.Abstractions;
using ScimSlim.EntityFramework.Entities;
using ScimSlim.Filtering;
using ScimSlim.Models;
using ScimSlim.Patching;

namespace ScimSlim.EntityFramework;

/// <summary>
/// EF Core-backed <see cref="IScimGroupStore"/> operating on
/// <see cref="ScimGroupEntity"/> rows in an <see cref="IScimDbContext"/>.
/// </summary>
public class EfScimGroupStore(IScimDbContext db) : IScimGroupStore
{
    public async Task<ScimGroup?> GetByIdAsync(string id)
    {
        var entity = await Query().FirstOrDefaultAsync(g => g.Id == id);
        return entity?.ToScim();
    }

    public async Task<ScimGroup?> GetByExternalIdAsync(string externalId)
    {
        var entity = await Query().FirstOrDefaultAsync(g => g.ExternalId == externalId);
        return entity?.ToScim();
    }

    public async Task<(IEnumerable<ScimGroup> Groups, int Total)> ListAsync(
        string? filter, int startIndex, int count)
    {
        var query = Query();

        if (ScimFilter.TryParse(filter, out var parsed) && parsed.Value.Operator == "eq")
        {
            var value = parsed.Value.Value;
            query = parsed.Value.Attribute.ToLowerInvariant() switch
            {
                "displayname" => query.Where(g => g.DisplayName == value),
                "externalid" => query.Where(g => g.ExternalId == value),
                _ => query,
            };
        }

        var total = await query.CountAsync();

        var entities = await query
            .OrderBy(g => g.Id)
            .Skip(Math.Max(0, startIndex - 1))
            .Take(count)
            .ToListAsync();

        return (entities.Select(e => e.ToScim()), total);
    }

    public async Task<ScimGroup> CreateAsync(ScimGroup group)
    {
        var entity = new ScimGroupEntity
        {
            Id = string.IsNullOrEmpty(group.Id) ? Guid.NewGuid().ToString() : group.Id,
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        entity.ApplyFrom(group);

        db.ScimGroups.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToScim();
    }

    public async Task<ScimGroup> UpdateAsync(string id, ScimGroup group)
    {
        var entity = await Query().FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException($"Group '{id}' not found.");

        entity.ApplyFrom(group);
        entity.LastModified = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return entity.ToScim();
    }

    public async Task ApplyPatchAsync(string id, ScimPatchRequest patch)
    {
        var entity = await Query().FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException($"Group '{id}' not found.");

        var group = entity.ToScim();
        ScimPatch.Apply(group, patch);
        entity.ApplyFrom(group);
        entity.LastModified = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await db.ScimGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null)
        {
            return;
        }

        db.ScimGroups.Remove(entity);
        await db.SaveChangesAsync();
    }

    private IQueryable<ScimGroupEntity> Query() => db.ScimGroups.Include(g => g.Members);
}

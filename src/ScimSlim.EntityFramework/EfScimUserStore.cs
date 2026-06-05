using Microsoft.EntityFrameworkCore;
using ScimSlim.Abstractions;
using ScimSlim.EntityFramework.Entities;
using ScimSlim.Filtering;
using ScimSlim.Models;
using ScimSlim.Patching;

namespace ScimSlim.EntityFramework;

/// <summary>
/// EF Core-backed <see cref="IScimUserStore"/> operating on
/// <see cref="ScimUserEntity"/> rows in an <see cref="IScimDbContext"/>.
/// </summary>
public class EfScimUserStore(IScimDbContext db) : IScimUserStore
{
    public async Task<ScimUser?> GetByIdAsync(string id)
    {
        var entity = await Query().FirstOrDefaultAsync(u => u.Id == id);
        return entity?.ToScim();
    }

    public async Task<ScimUser?> GetByExternalIdAsync(string externalId)
    {
        var entity = await Query().FirstOrDefaultAsync(u => u.ExternalId == externalId);
        return entity?.ToScim();
    }

    public async Task<ScimUser?> GetByUsernameAsync(string username)
    {
        var entity = await Query().FirstOrDefaultAsync(u => u.UserName == username);
        return entity?.ToScim();
    }

    public async Task<(IEnumerable<ScimUser> Users, int Total)> ListAsync(
        string? filter, int startIndex, int count)
    {
        var query = Query();

        if (ScimFilter.TryParse(filter, out var parsed) && parsed.Value.Operator == "eq")
        {
            var value = parsed.Value.Value;
            query = parsed.Value.Attribute.ToLowerInvariant() switch
            {
                "username" => query.Where(u => u.UserName == value),
                "externalid" => query.Where(u => u.ExternalId == value),
                _ => query,
            };
        }

        var total = await query.CountAsync();

        var entities = await query
            .OrderBy(u => u.Id)
            .Skip(Math.Max(0, startIndex - 1))
            .Take(count)
            .ToListAsync();

        return (entities.Select(e => e.ToScim()), total);
    }

    public async Task<ScimUser> CreateAsync(ScimUser user)
    {
        var entity = new ScimUserEntity
        {
            Id = string.IsNullOrEmpty(user.Id) ? Guid.NewGuid().ToString() : user.Id,
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        entity.ApplyFrom(user);

        db.ScimUsers.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToScim();
    }

    public async Task<ScimUser> UpdateAsync(string id, ScimUser user)
    {
        var entity = await Query().FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException($"User '{id}' not found.");

        entity.ApplyFrom(user);
        entity.LastModified = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return entity.ToScim();
    }

    public async Task ApplyPatchAsync(string id, ScimPatchRequest patch)
    {
        var entity = await Query().FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException($"User '{id}' not found.");

        var user = entity.ToScim();
        ScimPatch.Apply(user, patch);
        entity.ApplyFrom(user);
        entity.LastModified = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await db.ScimUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null)
        {
            return;
        }

        db.ScimUsers.Remove(entity);
        await db.SaveChangesAsync();
    }

    private IQueryable<ScimUserEntity> Query() => db.ScimUsers.Include(u => u.Emails);
}

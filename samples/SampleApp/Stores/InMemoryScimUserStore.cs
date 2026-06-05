using System.Collections.Concurrent;
using ScimSlim.Abstractions;
using ScimSlim.Filtering;
using ScimSlim.Models;
using ScimSlim.Patching;

namespace SampleApp.Stores;

/// <summary>
/// Demo <see cref="IScimUserStore"/> backed by an in-memory dictionary.
/// Replace with a real data store (e.g. ScimSlim.EntityFramework) in production.
/// </summary>
public class InMemoryScimUserStore : IScimUserStore
{
    private readonly ConcurrentDictionary<string, ScimUser> _users = new();

    public Task<ScimUser?> GetByIdAsync(string id) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<ScimUser?> GetByExternalIdAsync(string externalId) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.ExternalId == externalId));

    public Task<ScimUser?> GetByUsernameAsync(string username) =>
        Task.FromResult(_users.Values.FirstOrDefault(u =>
            string.Equals(u.UserName, username, StringComparison.OrdinalIgnoreCase)));

    public Task<(IEnumerable<ScimUser> Users, int Total)> ListAsync(
        string? filter, int startIndex, int count)
    {
        IEnumerable<ScimUser> query = _users.Values.OrderBy(u => u.Id);

        if (ScimFilter.TryParse(filter, out var parsed) && parsed.Value.Operator == "eq")
        {
            var value = parsed.Value.Value;
            query = parsed.Value.Attribute.ToLowerInvariant() switch
            {
                "username" => query.Where(u =>
                    string.Equals(u.UserName, value, StringComparison.OrdinalIgnoreCase)),
                "externalid" => query.Where(u => u.ExternalId == value),
                _ => query,
            };
        }

        var all = query.ToList();
        var page = all.Skip(Math.Max(0, startIndex - 1)).Take(count);
        return Task.FromResult(((IEnumerable<ScimUser>)page.ToList(), all.Count));
    }

    public Task<ScimUser> CreateAsync(ScimUser user)
    {
        user.Id = string.IsNullOrEmpty(user.Id) ? Guid.NewGuid().ToString() : user.Id;
        user.Meta = new ScimMeta
        {
            ResourceType = "User",
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<ScimUser> UpdateAsync(string id, ScimUser user)
    {
        user.Id = id;
        var existing = _users.GetValueOrDefault(id);
        user.Meta = new ScimMeta
        {
            ResourceType = "User",
            Created = existing?.Meta?.Created ?? DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        _users[id] = user;
        return Task.FromResult(user);
    }

    public Task ApplyPatchAsync(string id, ScimPatchRequest patch)
    {
        if (_users.TryGetValue(id, out var user))
        {
            ScimPatch.Apply(user, patch);
            user.Meta ??= new ScimMeta { ResourceType = "User" };
            user.Meta.LastModified = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _users.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

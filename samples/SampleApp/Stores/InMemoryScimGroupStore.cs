using System.Collections.Concurrent;
using ScimSlim.Abstractions;
using ScimSlim.Filtering;
using ScimSlim.Models;
using ScimSlim.Patching;

namespace SampleApp.Stores;

/// <summary>
/// Demo <see cref="IScimGroupStore"/> backed by an in-memory dictionary.
/// </summary>
public class InMemoryScimGroupStore : IScimGroupStore
{
    private readonly ConcurrentDictionary<string, ScimGroup> _groups = new();

    public Task<ScimGroup?> GetByIdAsync(string id) =>
        Task.FromResult(_groups.GetValueOrDefault(id));

    public Task<ScimGroup?> GetByExternalIdAsync(string externalId) =>
        Task.FromResult(_groups.Values.FirstOrDefault(g => g.ExternalId == externalId));

    public Task<(IEnumerable<ScimGroup> Groups, int Total)> ListAsync(
        string? filter, int startIndex, int count)
    {
        IEnumerable<ScimGroup> query = _groups.Values.OrderBy(g => g.Id);

        if (ScimFilter.TryParse(filter, out var parsed) && parsed.Value.Operator == "eq")
        {
            var value = parsed.Value.Value;
            query = parsed.Value.Attribute.ToLowerInvariant() switch
            {
                "displayname" => query.Where(g =>
                    string.Equals(g.DisplayName, value, StringComparison.OrdinalIgnoreCase)),
                "externalid" => query.Where(g => g.ExternalId == value),
                _ => query,
            };
        }

        var all = query.ToList();
        var page = all.Skip(Math.Max(0, startIndex - 1)).Take(count);
        return Task.FromResult(((IEnumerable<ScimGroup>)page.ToList(), all.Count));
    }

    public Task<ScimGroup> CreateAsync(ScimGroup group)
    {
        group.Id = string.IsNullOrEmpty(group.Id) ? Guid.NewGuid().ToString() : group.Id;
        group.Meta = new ScimMeta
        {
            ResourceType = "Group",
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        _groups[group.Id] = group;
        return Task.FromResult(group);
    }

    public Task<ScimGroup> UpdateAsync(string id, ScimGroup group)
    {
        group.Id = id;
        var existing = _groups.GetValueOrDefault(id);
        group.Meta = new ScimMeta
        {
            ResourceType = "Group",
            Created = existing?.Meta?.Created ?? DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        _groups[id] = group;
        return Task.FromResult(group);
    }

    public Task ApplyPatchAsync(string id, ScimPatchRequest patch)
    {
        if (_groups.TryGetValue(id, out var group))
        {
            ScimPatch.Apply(group, patch);
            group.Meta ??= new ScimMeta { ResourceType = "Group" };
            group.Meta.LastModified = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _groups.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

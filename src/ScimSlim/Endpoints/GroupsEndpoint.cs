using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScimSlim.Abstractions;
using ScimSlim.Http;
using ScimSlim.Models;

namespace ScimSlim.Endpoints;

/// <summary>
/// Minimal-API handlers for the SCIM <c>/Groups</c> resource.
/// </summary>
public static class GroupsEndpoint
{
    public static async Task<IResult> List(
        IScimGroupStore store,
        ScimOptions options,
        HttpContext ctx,
        [FromQuery] string? filter = null,
        [FromQuery] int? startIndex = null,
        [FromQuery] int? count = null)
    {
        var start = startIndex is > 0 ? startIndex.Value : 1;
        var take = count is >= 0 ? count.Value : options.DefaultPageSize;

        var (groups, total) = await store.ListAsync(filter, start, take);

        var resources = groups.ToList();
        foreach (var group in resources)
        {
            group.PopulateMeta(ScimMetaHelper.ChildUrl(ctx, group.Id));
        }

        return ScimResults.Ok(new ScimListResponse<ScimGroup>
        {
            TotalResults = total,
            StartIndex = start,
            ItemsPerPage = resources.Count,
            Resources = resources,
        });
    }

    public static async Task<IResult> Get(IScimGroupStore store, HttpContext ctx, string id)
    {
        var group = await store.GetByIdAsync(id);
        if (group is null)
        {
            return ScimResults.NotFound($"Group '{id}' not found.");
        }

        return ScimResults.Ok(group.PopulateMeta(ScimMetaHelper.CurrentUrl(ctx)));
    }

    public static async Task<IResult> Create(IScimGroupStore store, HttpContext ctx, ScimGroup? group)
    {
        if (group is null || string.IsNullOrWhiteSpace(group.DisplayName))
        {
            return ScimResults.BadRequest("A displayName is required.", "invalidValue");
        }

        if (!string.IsNullOrEmpty(group.ExternalId) &&
            await store.GetByExternalIdAsync(group.ExternalId) is not null)
        {
            return ScimResults.Conflict(
                $"Group with externalId '{group.ExternalId}' already exists.");
        }

        var created = await store.CreateAsync(group);
        var location = ScimMetaHelper.ChildUrl(ctx, created.Id);
        return ScimResults.Created(created.PopulateMeta(location), location);
    }

    public static async Task<IResult> Replace(
        IScimGroupStore store, HttpContext ctx, string id, ScimGroup? group)
    {
        if (group is null)
        {
            return ScimResults.BadRequest("A group body is required.");
        }

        if (await store.GetByIdAsync(id) is null)
        {
            return ScimResults.NotFound($"Group '{id}' not found.");
        }

        var updated = await store.UpdateAsync(id, group);
        return ScimResults.Ok(updated.PopulateMeta(ScimMetaHelper.CurrentUrl(ctx)));
    }

    public static async Task<IResult> Patch(
        IScimGroupStore store, ScimOptions options, HttpContext ctx,
        string id, ScimPatchRequest? patch)
    {
        if (!options.SupportPatch)
        {
            return ScimResults.Error(
                StatusCodes.Status501NotImplemented, "PATCH is not supported.");
        }

        if (patch is null)
        {
            return ScimResults.BadRequest("A PatchOp body is required.");
        }

        if (await store.GetByIdAsync(id) is null)
        {
            return ScimResults.NotFound($"Group '{id}' not found.");
        }

        await store.ApplyPatchAsync(id, patch);

        var updated = await store.GetByIdAsync(id);
        return updated is null
            ? ScimResults.NotFound($"Group '{id}' not found.")
            : ScimResults.Ok(updated.PopulateMeta(ScimMetaHelper.CurrentUrl(ctx)));
    }

    public static async Task<IResult> Delete(IScimGroupStore store, string id)
    {
        if (await store.GetByIdAsync(id) is null)
        {
            return ScimResults.NotFound($"Group '{id}' not found.");
        }

        await store.DeleteAsync(id);
        return ScimResults.NoContent();
    }
}

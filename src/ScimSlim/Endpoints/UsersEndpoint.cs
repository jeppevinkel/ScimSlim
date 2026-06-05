using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScimSlim.Abstractions;
using ScimSlim.Http;
using ScimSlim.Models;

namespace ScimSlim.Endpoints;

/// <summary>
/// Minimal-API handlers for the SCIM <c>/Users</c> resource.
/// </summary>
public static class UsersEndpoint
{
    public static async Task<IResult> List(
        IScimUserStore store,
        ScimOptions options,
        HttpContext ctx,
        [FromQuery] string? filter = null,
        [FromQuery] int? startIndex = null,
        [FromQuery] int? count = null)
    {
        var start = startIndex is > 0 ? startIndex.Value : 1;
        var take = count is >= 0 ? count.Value : options.DefaultPageSize;

        var (users, total) = await store.ListAsync(filter, start, take);

        var resources = users.ToList();
        foreach (var user in resources)
        {
            user.PopulateMeta(ScimMetaHelper.ChildUrl(ctx, user.Id));
        }

        return ScimResults.Ok(new ScimListResponse<ScimUser>
        {
            TotalResults = total,
            StartIndex = start,
            ItemsPerPage = resources.Count,
            Resources = resources,
        });
    }

    public static async Task<IResult> Get(IScimUserStore store, HttpContext ctx, string id)
    {
        var user = await store.GetByIdAsync(id);
        if (user is null)
        {
            return ScimResults.NotFound($"User '{id}' not found.");
        }

        return ScimResults.Ok(user.PopulateMeta(ScimMetaHelper.CurrentUrl(ctx)));
    }

    public static async Task<IResult> Create(IScimUserStore store, HttpContext ctx, ScimUser? user)
    {
        if (user is null || string.IsNullOrWhiteSpace(user.UserName))
        {
            return ScimResults.BadRequest("A userName is required.", "invalidValue");
        }

        // Idempotency: Authentik re-syncs, so a duplicate create maps to the existing user.
        var existing = await FindExisting(store, user);
        if (existing is not null)
        {
            return ScimResults.Conflict(
                $"User with userName '{user.UserName}' already exists.");
        }

        var created = await store.CreateAsync(user);
        var location = ScimMetaHelper.ChildUrl(ctx, created.Id);
        return ScimResults.Created(created.PopulateMeta(location), location);
    }

    public static async Task<IResult> Replace(
        IScimUserStore store, HttpContext ctx, string id, ScimUser? user)
    {
        if (user is null)
        {
            return ScimResults.BadRequest("A user body is required.");
        }

        if (await store.GetByIdAsync(id) is null)
        {
            return ScimResults.NotFound($"User '{id}' not found.");
        }

        var updated = await store.UpdateAsync(id, user);
        return ScimResults.Ok(updated.PopulateMeta(ScimMetaHelper.CurrentUrl(ctx)));
    }

    public static async Task<IResult> Patch(
        IScimUserStore store, ScimOptions options, HttpContext ctx,
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
            return ScimResults.NotFound($"User '{id}' not found.");
        }

        await store.ApplyPatchAsync(id, patch);

        var updated = await store.GetByIdAsync(id);
        return updated is null
            ? ScimResults.NotFound($"User '{id}' not found.")
            : ScimResults.Ok(updated.PopulateMeta(ScimMetaHelper.CurrentUrl(ctx)));
    }

    public static async Task<IResult> Delete(IScimUserStore store, string id)
    {
        if (await store.GetByIdAsync(id) is null)
        {
            return ScimResults.NotFound($"User '{id}' not found.");
        }

        await store.DeleteAsync(id);
        return ScimResults.NoContent();
    }

    private static async Task<ScimUser?> FindExisting(IScimUserStore store, ScimUser user)
    {
        if (!string.IsNullOrEmpty(user.ExternalId) &&
            await store.GetByExternalIdAsync(user.ExternalId) is { } byExternal)
        {
            return byExternal;
        }

        return await store.GetByUsernameAsync(user.UserName);
    }
}

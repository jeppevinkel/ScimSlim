using Microsoft.AspNetCore.Http;
using ScimSlim.Models;

namespace ScimSlim.Http;

/// <summary>
/// Builds resource <c>Location</c> URLs from the current request and fills in
/// the <c>meta</c> block on outgoing resources.
/// </summary>
public static class ScimMetaHelper
{
    private static string BaseUrl(HttpContext ctx) =>
        $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}";

    /// <summary>The absolute URL of the resource at the current request path.</summary>
    public static string CurrentUrl(HttpContext ctx) =>
        $"{BaseUrl(ctx)}{ctx.Request.Path}";

    /// <summary>The absolute URL of a child resource under the current collection path.</summary>
    public static string ChildUrl(HttpContext ctx, string id) =>
        $"{BaseUrl(ctx)}{ctx.Request.Path.Value?.TrimEnd('/')}/{id}";

    public static ScimUser PopulateMeta(this ScimUser user, string location)
    {
        user.Meta ??= new ScimMeta();
        user.Meta.ResourceType = "User";
        user.Meta.Location = location;
        return user;
    }

    public static ScimGroup PopulateMeta(this ScimGroup group, string location)
    {
        group.Meta ??= new ScimMeta();
        group.Meta.ResourceType = "Group";
        group.Meta.Location = location;
        return group;
    }
}

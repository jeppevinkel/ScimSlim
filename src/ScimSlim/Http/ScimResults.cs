using Microsoft.AspNetCore.Http;
using ScimSlim.Models;

namespace ScimSlim.Http;

/// <summary>
/// Helpers that produce <see cref="IResult"/> responses with the
/// <c>application/scim+json</c> content type and SCIM error envelopes.
/// </summary>
public static class ScimResults
{
    public static IResult Ok<T>(T value) =>
        new ScimJsonResult<T>(value, StatusCodes.Status200OK);

    public static IResult Created<T>(T value, string location) =>
        new ScimJsonResult<T>(value, StatusCodes.Status201Created, location);

    public static IResult NoContent() => Results.StatusCode(StatusCodes.Status204NoContent);

    public static IResult Error(int status, string? detail, string? scimType = null) =>
        new ScimJsonResult<ScimError>(
            new ScimError { Status = status, Detail = detail, ScimType = scimType },
            status);

    public static IResult NotFound(string? detail = null) =>
        Error(StatusCodes.Status404NotFound, detail ?? "Resource not found.");

    public static IResult BadRequest(string? detail = null, string? scimType = null) =>
        Error(StatusCodes.Status400BadRequest, detail ?? "Malformed request.", scimType);

    public static IResult Conflict(string? detail = null) =>
        Error(StatusCodes.Status409Conflict, detail ?? "Resource already exists.", "uniqueness");

    /// <summary>
    /// Writes a value as <c>application/scim+json</c> with an explicit status code
    /// and, optionally, a <c>Location</c> header.
    /// </summary>
    private sealed class ScimJsonResult<T>(T value, int statusCode, string? location = null)
        : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = statusCode;
            if (location is not null)
            {
                httpContext.Response.Headers.Location = location;
            }

            await httpContext.Response.WriteAsJsonAsync(
                value, ScimJson.Options, ScimJson.ContentType);
        }
    }
}

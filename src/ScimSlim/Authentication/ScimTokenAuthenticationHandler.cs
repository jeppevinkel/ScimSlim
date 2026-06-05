using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ScimSlim.Authentication;

/// <summary>
/// Authentication handler that validates the <c>Authorization: Bearer &lt;token&gt;</c>
/// header against <see cref="ScimOptions.StaticToken"/>. This is the simplest auth
/// option Authentik's SCIM provider supports.
/// </summary>
public sealed class ScimTokenAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string BearerPrefix = "Bearer ";

    private readonly ScimOptions _scimOptions;

    public ScimTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ScimOptions scimOptions)
        : base(options, logger, encoder)
    {
        _scimOptions = scimOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(_scimOptions.StaticToken))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("SCIM static token is not configured."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = authHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader[BearerPrefix.Length..].Trim();
        if (!FixedTimeEquals(token, _scimOptions.StaticToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid SCIM token."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "scim-client")], Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}

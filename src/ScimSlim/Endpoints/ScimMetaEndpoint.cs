using Microsoft.AspNetCore.Http;
using ScimSlim.Http;
using ScimSlim.Models;

namespace ScimSlim.Endpoints;

/// <summary>
/// Handler for SCIM service metadata endpoints (<c>/ServiceProviderConfig</c>).
/// </summary>
public static class ScimMetaEndpoint
{
    public static IResult ServiceProviderConfig(ScimOptions options)
    {
        var config = new ScimServiceProviderConfig
        {
            Patch = new ScimSupported { Supported = options.SupportPatch },
            Filter = new ScimFilterSupported
            {
                Supported = options.SupportFiltering,
                MaxResults = 200,
            },
            Bulk = new ScimBulkSupported { Supported = false },
            ChangePassword = new ScimSupported { Supported = false },
            Sort = new ScimSupported { Supported = false },
            ETag = new ScimSupported { Supported = false },
            AuthenticationSchemes =
            [
                new ScimAuthenticationScheme
                {
                    Type = "oauthbearertoken",
                    Name = "OAuth Bearer Token",
                    Description = "Authentication via a static Bearer token.",
                    Primary = true,
                },
            ],
        };

        return ScimResults.Ok(config);
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace DeomaBlog.Web.Security;

/// <summary>
/// Minimal write-API protection: a shared key in the <c>X-Api-Key</c> header.
/// A full Identity/OpenID Connect setup would be overkill for a one-author blog, but the
/// write endpoints still must not be open to the internet.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = configuration[ApiKeyAuthenticationDefaults.ConfigurationKey];

        if (string.IsNullOrWhiteSpace(expected))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Write API is disabled",
                Detail = $"Configure {ApiKeyAuthenticationDefaults.ConfigurationKey} to enable it.",
                Status = StatusCodes.Status503ServiceUnavailable,
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };

            return;
        }

        var provided = context.HttpContext.Request.Headers[ApiKeyAuthenticationDefaults.HeaderName].ToString();

        if (!FixedTimeEquals(expected, provided))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Invalid API key",
                Status = StatusCodes.Status401Unauthorized,
            });

            return;
        }

        await next();
    }

    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

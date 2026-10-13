using DeomaBlog.Application.Exceptions;
using DeomaBlog.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DeomaBlog.Web.Filters;

/// <summary>
/// Translates application exceptions into RFC 7807 problem responses — but only for the
/// JSON API surface. MVC pages keep using the /error page pipeline, so a browser never
/// receives a raw JSON body for a broken view.
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        var (statusCode, title) = context.Exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            DomainException => (StatusCodes.Status400BadRequest, "Request violates a domain rule"),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = title,
            Detail = context.Exception.Message,
            Status = statusCode,
            Instance = context.HttpContext.Request.Path,
        })
        {
            StatusCode = statusCode,
        };

        context.ExceptionHandled = true;
    }
}

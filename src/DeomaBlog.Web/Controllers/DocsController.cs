using Microsoft.AspNetCore.Mvc;

namespace DeomaBlog.Web.Controllers;

/// <summary>
/// Human friendly endpoint documentation. Swagger UI is rendered from a CDN and pointed at the
/// OpenAPI document the app produces itself (<c>/openapi/v1.json</c>), so the deployment carries
/// no extra JavaScript bundle.
/// </summary>
public sealed class DocsController : Controller
{
    [HttpGet("/api/docs")]
    public IActionResult Index()
    {
        ViewData["Title"] = "API";

        return View();
    }
}

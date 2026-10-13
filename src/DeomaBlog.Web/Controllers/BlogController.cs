using DeomaBlog.Application.Exceptions;
using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;
using DeomaBlog.Web.Content;
using DeomaBlog.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeomaBlog.Web.Controllers;

/// <summary>
/// Server-rendered pages. The controller does three things only: read the query string,
/// call one use case, and pick a view — all HTML shaping lives in the Razor templates.
/// </summary>
public sealed class BlogController : Controller
{
    private const int PageSize = 6;

    private readonly IBlogQueryService _blog;

    public BlogController(IBlogQueryService blog) => _blog = blog;

    [HttpGet("/")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var latest = await _blog.GetLatestAsync(3, cancellationToken);
        var tags = await _blog.GetTagsAsync(cancellationToken);
        var all = await _blog.GetPostsAsync(new PostFilter { PageSize = PostFilter.MaxPageSize }, cancellationToken);

        ViewData["Title"] = "About";
        ViewData["Description"] =
            "Demid Andreev — .NET developer. Notes on C#, ASP.NET Core and clean architecture, written as I learn them.";

        return View(new HomeViewModel(
            latest,
            tags,
            all.TotalCount,
            all.Items.Sum(post => post.ReadingTimeMinutes)));
    }

    [HttpGet("/posts")]
    public async Task<IActionResult> Posts(
        int page = 1,
        string? tag = null,
        string? q = null,
        string sort = "newest",
        CancellationToken cancellationToken = default)
    {
        var order = sort switch
        {
            "oldest" => PostSortOrder.OldestFirst,
            "popular" => PostSortOrder.MostViewed,
            _ => PostSortOrder.NewestFirst,
        };

        var posts = await _blog.GetPostsAsync(
            new PostFilter { Page = page, PageSize = PageSize, Tag = tag, Search = q, Sort = order },
            cancellationToken);

        var tags = await _blog.GetTagsAsync(cancellationToken);

        ViewData["Title"] = "Posts";
        ViewData["Description"] = "All posts about C#, ASP.NET Core, EF Core and software design.";

        return View(new PostListViewModel(posts, tags, tag, q, order));
    }

    [HttpGet("/posts/{slug}")]
    public async Task<IActionResult> Post(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _blog.GetPostAsync(slug, cancellationToken: cancellationToken);
            var related = await _blog.GetRelatedAsync(slug, 3, cancellationToken);

            await _blog.RegisterViewAsync(slug, cancellationToken);

            ViewData["Title"] = post.Title;
            ViewData["Description"] = post.Summary;

            return View(new PostPageViewModel(post, related));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("/projects")]
    public IActionResult Projects()
    {
        ViewData["Title"] = "Projects";
        ViewData["Description"] = "Pet projects by Demid Andreev: .NET services, APIs and front-end experiments.";

        return View(SiteContent.Projects);
    }

    [HttpGet("/work-experience")]
    public IActionResult Experience()
    {
        ViewData["Title"] = "Work_Experience";
        ViewData["Description"] = "Education, practice and the skills Demid Andreev works with.";

        return View(SiteContent.Experience);
    }

    [HttpGet("/error")]
    [HttpGet("/error/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int code = StatusCodes.Status500InternalServerError)
    {
        var (title, message) = code switch
        {
            StatusCodes.Status404NotFound =>
                ("404 — nothing here", "The page you asked for does not exist. Try the posts archive."),
            StatusCodes.Status400BadRequest =>
                ("400 — bad request", "Something in the request did not make sense."),
            StatusCodes.Status503ServiceUnavailable =>
                ("503 — temporarily unavailable", "The blog is restarting. Give it a few seconds."),
            _ => ("500 — something broke", "An unexpected error happened. The logs know more than this page does."),
        };

        Response.StatusCode = code;
        ViewData["Title"] = title;

        return View(new ErrorViewModel(code, title, message));
    }
}

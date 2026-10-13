using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;
using DeomaBlog.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace DeomaBlog.Web.Controllers.Api;

/// <summary>
/// JSON API over the same use cases the pages use.
/// Read endpoints are public; write endpoints require the admin key.
/// </summary>
[ApiController]
[Route("api/posts")]
[Produces("application/json")]
public sealed class PostsApiController : ControllerBase
{
    private readonly IBlogQueryService _blog;
    private readonly IBlogAuthoringService _authoring;

    public PostsApiController(IBlogQueryService blog, IBlogAuthoringService authoring)
    {
        _blog = blog;
        _authoring = authoring;
    }

    /// <summary>Paged list of published posts.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<PostSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PostSummaryDto>>> GetPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6,
        [FromQuery] string? tag = null,
        [FromQuery] string? q = null,
        [FromQuery] PostSortOrder sort = PostSortOrder.NewestFirst,
        CancellationToken cancellationToken = default)
        => Ok(await _blog.GetPostsAsync(
            new PostFilter { Page = page, PageSize = pageSize, Tag = tag, Search = q, Sort = sort },
            cancellationToken));

    /// <summary>One post with its rendered HTML body.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType<PostDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDetailsDto>> GetPost(string slug, CancellationToken cancellationToken)
        => Ok(await _blog.GetPostAsync(slug, cancellationToken: cancellationToken));

    /// <summary>Posts related to the given one, by shared tags.</summary>
    [HttpGet("{slug}/related")]
    public async Task<ActionResult<IReadOnlyList<PostSummaryDto>>> GetRelated(
        string slug,
        [FromQuery] int count = 3,
        CancellationToken cancellationToken = default)
        => Ok(await _blog.GetRelatedAsync(slug, count, cancellationToken));

    /// <summary>Creates a post. Requires the <c>X-Api-Key</c> header.</summary>
    [HttpPost]
    [RequireApiKey]
    [ProducesResponseType<PostDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PostDetailsDto>> Create(
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _authoring.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetPost), new { slug = created.Slug }, created);
    }

    /// <summary>Edits title, summary, body and tags of an existing post.</summary>
    [HttpPut("{slug}")]
    [RequireApiKey]
    [ProducesResponseType<PostDetailsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostDetailsDto>> Update(
        string slug,
        [FromBody] UpdatePostRequest request,
        CancellationToken cancellationToken)
        => Ok(await _authoring.UpdateAsync(slug, request, cancellationToken));

    /// <summary>Publishes a draft.</summary>
    [HttpPost("{slug}/publish")]
    [RequireApiKey]
    public async Task<ActionResult<PostDetailsDto>> Publish(string slug, CancellationToken cancellationToken)
        => Ok(await _authoring.PublishAsync(slug, cancellationToken));

    /// <summary>Archives a published post without deleting it.</summary>
    [HttpPost("{slug}/archive")]
    [RequireApiKey]
    public async Task<ActionResult<PostDetailsDto>> Archive(string slug, CancellationToken cancellationToken)
        => Ok(await _authoring.ArchiveAsync(slug, cancellationToken));

    /// <summary>Deletes a post permanently.</summary>
    [HttpDelete("{slug}")]
    [RequireApiKey]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        await _authoring.DeleteAsync(slug, cancellationToken);
        return NoContent();
    }
}

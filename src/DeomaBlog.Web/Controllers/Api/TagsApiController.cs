using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;
using Microsoft.AspNetCore.Mvc;

namespace DeomaBlog.Web.Controllers.Api;

/// <summary>Tags with the number of published posts using them.</summary>
[ApiController]
[Route("api/tags")]
[Produces("application/json")]
public sealed class TagsApiController : ControllerBase
{
    private readonly IBlogQueryService _blog;

    public TagsApiController(IBlogQueryService blog) => _blog = blog;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetTags(CancellationToken cancellationToken)
        => Ok(await _blog.GetTagsAsync(cancellationToken));
}

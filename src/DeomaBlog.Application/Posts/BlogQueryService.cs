using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Exceptions;
using DeomaBlog.Application.Mapping;
using DeomaBlog.Application.Models;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.Application.Posts;

/// <inheritdoc cref="IBlogQueryService" />
public sealed class BlogQueryService : IBlogQueryService
{
    private readonly IPostRepository _posts;
    private readonly ITagRepository _tags;
    private readonly IMarkdownRenderer _markdown;
    private readonly IUnitOfWork _unitOfWork;

    public BlogQueryService(
        IPostRepository posts,
        ITagRepository tags,
        IMarkdownRenderer markdown,
        IUnitOfWork unitOfWork)
    {
        _posts = posts;
        _tags = tags;
        _markdown = markdown;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<PostSummaryDto>> GetPostsAsync(
        PostFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await _posts.GetPagedAsync(filter.Normalized(), cancellationToken);

        return new PagedResult<PostSummaryDto>(
            page.Items.Select(PostMapper.ToSummary).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<PostDetailsDto> GetPostAsync(
        string slug,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetBySlugAsync(ParseOrNotFound(slug), includeUnpublished, cancellationToken)
                   ?? throw new NotFoundException(nameof(Post), slug);

        return PostMapper.ToDetails(post, _markdown);
    }

    public async Task<IReadOnlyList<PostSummaryDto>> GetLatestAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var posts = await _posts.GetLatestPublishedAsync(Math.Clamp(count, 1, 20), cancellationToken);
        return posts.Select(PostMapper.ToSummary).ToList();
    }

    public async Task<IReadOnlyList<PostSummaryDto>> GetRelatedAsync(
        string slug,
        int count = 3,
        CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetBySlugAsync(ParseOrNotFound(slug), false, cancellationToken)
                   ?? throw new NotFoundException(nameof(Post), slug);

        var related = await _posts.GetRelatedAsync(post, Math.Clamp(count, 1, 10), cancellationToken);
        return related.Select(PostMapper.ToSummary).ToList();
    }

    public async Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _tags.GetWithPublishedCountsAsync(cancellationToken);
        return tags.Select(PostMapper.ToTag).ToList();
    }

    public async Task RegisterViewAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (!Slug.IsValid(slug))
        {
            return;
        }

        var post = await _posts.GetBySlugAsync(Slug.Create(slug), false, cancellationToken);

        if (post is null)
        {
            return;
        }

        post.RegisterView();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Slugs arrive from the URL, so a malformed one is a "not found", not a validation error:
    /// /api/posts/Not-A-Post should answer 404, not 400.
    /// </summary>
    private static Slug ParseOrNotFound(string slug)
        => Slug.IsValid(slug) ? Slug.Create(slug) : throw new NotFoundException(nameof(Post), slug);
}

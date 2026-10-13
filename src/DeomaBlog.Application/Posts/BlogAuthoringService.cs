using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Exceptions;
using DeomaBlog.Application.Mapping;
using DeomaBlog.Application.Models;
using DeomaBlog.Domain.Common;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.Application.Posts;

/// <inheritdoc cref="IBlogAuthoringService" />
public sealed class BlogAuthoringService : IBlogAuthoringService
{
    private readonly IPostRepository _posts;
    private readonly ITagRepository _tags;
    private readonly IMarkdownRenderer _markdown;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BlogAuthoringService(
        IPostRepository posts,
        ITagRepository tags,
        IMarkdownRenderer markdown,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _posts = posts;
        _tags = tags;
        _markdown = markdown;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PostDetailsDto> CreateAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var slug = await ResolveUniqueSlugAsync(request.Slug, request.Title, cancellationToken);
        var post = Post.Create(request.Title, request.Summary, request.ContentMarkdown, slug);

        await AttachTagsAsync(post, request.Tags, cancellationToken);

        if (request.PublishNow)
        {
            post.Publish(_clock.UtcNow, request.CoverImageUrl);
        }

        await _posts.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PostMapper.ToDetails(post, _markdown);
    }

    public async Task<PostDetailsDto> UpdateAsync(
        string slug,
        UpdatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var post = await GetRequiredAsync(slug, cancellationToken);

        post.Edit(request.Title, request.Summary, request.ContentMarkdown, _clock.UtcNow);

        foreach (var existingTag in post.PostTags.Select(postTag => postTag.Tag.Slug).ToList())
        {
            post.RemoveTag(existingTag);
        }

        await AttachTagsAsync(post, request.Tags, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PostMapper.ToDetails(post, _markdown);
    }

    public async Task<PostDetailsDto> PublishAsync(string slug, CancellationToken cancellationToken = default)
    {
        var post = await GetRequiredAsync(slug, cancellationToken);

        if (!post.IsPublished)
        {
            post.Publish(_clock.UtcNow, post.CoverImageUrl);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return PostMapper.ToDetails(post, _markdown);
    }

    public async Task<PostDetailsDto> ArchiveAsync(string slug, CancellationToken cancellationToken = default)
    {
        var post = await GetRequiredAsync(slug, cancellationToken);
        post.Archive(_clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PostMapper.ToDetails(post, _markdown);
    }

    public async Task DeleteAsync(string slug, CancellationToken cancellationToken = default)
    {
        var post = await GetRequiredAsync(slug, cancellationToken);
        _posts.Remove(post);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Post> GetRequiredAsync(string slug, CancellationToken cancellationToken)
    {
        if (!Slug.IsValid(slug))
        {
            throw new NotFoundException(nameof(Post), slug);
        }

        var parsed = Slug.Create(slug);

        return await _posts.GetBySlugAsync(parsed, includeUnpublished: true, cancellationToken)
               ?? throw new NotFoundException(nameof(Post), slug);
    }

    private async Task AttachTagsAsync(Post post, IReadOnlyList<string>? names, CancellationToken cancellationToken)
    {
        if (names is null || names.Count == 0)
        {
            return;
        }

        var tags = await _tags.ResolveAsync(names.Where(name => !string.IsNullOrWhiteSpace(name)), cancellationToken);

        foreach (var tag in tags)
        {
            post.AssignTag(tag, _clock.UtcNow);
        }
    }

    /// <summary>Turns "my new post" into "my-new-post", "my-new-post-2", … so slugs stay unique.</summary>
    private async Task<Slug> ResolveUniqueSlugAsync(string? requested, string title, CancellationToken cancellationToken)
    {
        var baseSlug = string.IsNullOrWhiteSpace(requested) ? Slug.FromTitle(title) : Slug.FromTitle(requested);

        if (!await _posts.SlugExistsAsync(baseSlug, null, cancellationToken))
        {
            return baseSlug;
        }

        for (var suffix = 2; suffix < 100; suffix++)
        {
            var candidate = Slug.Create($"{baseSlug.Value}-{suffix}");

            if (!await _posts.SlugExistsAsync(candidate, null, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException($"Cannot build a unique slug for '{title}'.");
    }
}

using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Models;
using DeomaBlog.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace DeomaBlog.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IPostRepository" />
public sealed class PostRepository : IPostRepository
{
    private readonly BlogDbContext _context;

    public PostRepository(BlogDbContext context) => _context = context;

    public async Task<PagedResult<Post>> GetPagedAsync(
        PostFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(Query(noTracking: true), filter);

        var total = await query.CountAsync(cancellationToken);
        var items = await Sort(query, filter.Sort)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Post>(items, filter.Page, filter.PageSize, total);
    }

    public Task<Post?> GetBySlugAsync(
        Slug slug,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        var query = Query();

        if (!includeUnpublished)
        {
            query = query.Where(IsVisible);
        }

        return query.FirstOrDefaultAsync(post => post.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetLatestPublishedAsync(
        int count,
        CancellationToken cancellationToken = default)
        => await Query(noTracking: true)
            .Where(IsVisible)
            .OrderByDescending(post => post.PublishedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Post>> GetRelatedAsync(
        Post post,
        int count,
        CancellationToken cancellationToken = default)
    {
        var tagIds = post.PostTags.Select(postTag => postTag.TagId).ToArray();

        if (tagIds.Length == 0)
        {
            return await GetLatestPublishedAsync(count, cancellationToken);
        }

        return await Query(noTracking: true)
            .Where(IsVisible)
            .Where(candidate => candidate.Id != post.Id)
            .Where(candidate => candidate.PostTags.Any(postTag => tagIds.Contains(postTag.TagId)))
            .OrderByDescending(candidate => candidate.PostTags.Count(postTag => tagIds.Contains(postTag.TagId)))
            .ThenByDescending(candidate => candidate.PublishedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(
        Slug slug,
        Guid? exceptPostId = null,
        CancellationToken cancellationToken = default)
        => _context.Posts
            .AsNoTracking()
            .AnyAsync(
                post => post.Slug == slug && (exceptPostId == null || post.Id != exceptPostId),
                cancellationToken);

    public async Task AddAsync(Post post, CancellationToken cancellationToken = default)
        => await _context.Posts.AddAsync(post, cancellationToken);

    public void Remove(Post post) => _context.Posts.Remove(post);

    private IQueryable<Post> Query(bool noTracking = false)
    {
        var query = _context.Posts
            .Include(post => post.PostTags)
            .ThenInclude(postTag => postTag.Tag)
            .AsQueryable();

        return noTracking ? query.AsNoTracking() : query;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Post, bool>> IsVisible =
        post => post.Status == PostStatus.Published && post.PublishedAt != null;

    private static IQueryable<Post> ApplyFilter(IQueryable<Post> query, PostFilter filter)
    {
        if (!filter.IncludeUnpublished)
        {
            query = query.Where(IsVisible);
        }

        if (filter.Tag is not null)
        {
            var tag = Slug.Create(filter.Tag);
            query = query.Where(post => post.PostTags.Any(postTag => postTag.Tag.Slug == tag));
        }

        if (filter.Search is not null)
        {
            var pattern = $"%{filter.Search}%";
            query = query.Where(post =>
                EF.Functions.Like(post.Title, pattern)
                || EF.Functions.Like(post.Summary, pattern)
                || EF.Functions.Like(post.ContentMarkdown, pattern));
        }

        return query;
    }

    private static IOrderedQueryable<Post> Sort(IQueryable<Post> query, PostSortOrder sort) => sort switch
    {
        PostSortOrder.OldestFirst => query.OrderBy(post => post.PublishedAt),
        PostSortOrder.MostViewed => query.OrderByDescending(post => post.ViewCount)
            .ThenByDescending(post => post.PublishedAt),
        _ => query.OrderByDescending(post => post.PublishedAt),
    };
}

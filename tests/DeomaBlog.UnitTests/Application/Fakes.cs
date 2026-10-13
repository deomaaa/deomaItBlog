using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Models;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.UnitTests.Application;

/// <summary>In-memory stand-in for EF Core: enough behaviour to exercise the use cases.</summary>
internal sealed class FakePostRepository : IPostRepository
{
    private readonly List<Post> _posts;

    public FakePostRepository(IEnumerable<Post> posts) => _posts = [.. posts];

    public PostFilter? LastFilter { get; private set; }

    public Task<PagedResult<Post>> GetPagedAsync(PostFilter filter, CancellationToken cancellationToken = default)
    {
        LastFilter = filter.Normalized();

        var query = _posts
            .Where(post => filter.IncludeUnpublished || post.IsPublished)
            .Where(post => filter.Tag is null
                || post.PostTags.Any(postTag => postTag.Tag.Slug.Value == filter.Tag))
            .Where(post => filter.Search is null
                || post.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || post.ContentMarkdown.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(post => post.PublishedAt)
            .ToList();

        var page = query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Post>(page, filter.Page, filter.PageSize, query.Count));
    }

    public Task<Post?> GetBySlugAsync(
        Slug slug,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_posts.FirstOrDefault(post =>
            post.Slug.Equals(slug) && (includeUnpublished || post.IsPublished)));

    public Task<IReadOnlyList<Post>> GetLatestPublishedAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Post>>(_posts
            .Where(post => post.IsPublished)
            .OrderByDescending(post => post.PublishedAt)
            .Take(count)
            .ToList());

    public Task<IReadOnlyList<Post>> GetRelatedAsync(
        Post post,
        int count,
        CancellationToken cancellationToken = default)
    {
        var slugs = post.PostTags.Select(postTag => postTag.Tag.Slug).ToHashSet();

        return Task.FromResult<IReadOnlyList<Post>>(_posts
            .Where(candidate => candidate.Id != post.Id && candidate.IsPublished)
            .Where(candidate => candidate.PostTags.Any(postTag => slugs.Contains(postTag.Tag.Slug)))
            .Take(count)
            .ToList());
    }

    public Task<bool> SlugExistsAsync(
        Slug slug,
        Guid? exceptPostId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_posts.Any(post => post.Slug.Equals(slug) && post.Id != exceptPostId));

    public Task AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        _posts.Add(post);
        return Task.CompletedTask;
    }

    public void Remove(Post post) => _posts.Remove(post);
}

internal sealed class FakeTagRepository : ITagRepository
{
    private readonly List<TagWithCount> _tags;

    public FakeTagRepository(IEnumerable<TagWithCount> tags) => _tags = [.. tags];

    public Task<IReadOnlyList<TagWithCount>> GetWithPublishedCountsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TagWithCount>>(_tags);

    public Task<Tag?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
        => Task.FromResult<Tag?>(null);

    public Task<IReadOnlyList<Tag>> ResolveAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Tag>>(names.Select(Tag.Create).ToList());
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
}

/// <summary>Deterministic "renderer" so assertions never depend on Markdig output.</summary>
internal sealed class FakeMarkdownRenderer : IMarkdownRenderer
{
    public string ToHtml(string markdown) => $"<p>{markdown}</p>";

    public string ToPlainText(string markdown, int maxLength = 160)
        => markdown.Length <= maxLength ? markdown : markdown[..maxLength];
}

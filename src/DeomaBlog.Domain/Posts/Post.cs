using DeomaBlog.Domain.Common;

namespace DeomaBlog.Domain.Posts;

/// <summary>
/// Aggregate root: a blog post. All mutations go through methods that keep the
/// invariants (title/summary/content non-empty, publishing requires a date, slug stays unique
/// at the persistence level) so an invalid post cannot exist in memory.
/// </summary>
public sealed class Post : Entity
{
    private readonly List<PostTag> _postTags = [];

    private Post()
    {
        Title = string.Empty;
        Slug = Slug.Create("draft");
        Summary = string.Empty;
        ContentMarkdown = string.Empty;
    }

    private Post(string title, string summary, string contentMarkdown, Slug slug)
    {
        Title = title;
        Summary = summary;
        ContentMarkdown = contentMarkdown;
        Slug = slug;
        Status = PostStatus.Draft;
    }

    public string Title { get; private set; }

    public Slug Slug { get; private set; }

    public string Summary { get; private set; }

    public string ContentMarkdown { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public PostStatus Status { get; private set; }

    /// <summary>Set when the post first becomes publicly visible.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public int ViewCount { get; private set; }

    public IReadOnlyCollection<PostTag> PostTags => _postTags.AsReadOnly();

    public ReadingTime ReadingTime => ReadingTime.FromMarkdown(ContentMarkdown);

    public bool IsPublished => Status == PostStatus.Published && PublishedAt is not null;

    public static Post Create(
        string title,
        string summary,
        string contentMarkdown,
        Slug? slug = null)
    {
        var post = new Post(
            Guard.NotNullOrWhiteSpace(title, nameof(title), 200),
            Guard.NotNullOrWhiteSpace(summary, nameof(summary), 500),
            Guard.NotNullOrWhiteSpace(contentMarkdown, nameof(contentMarkdown), 200_000),
            slug ?? Slug.FromTitle(title));

        return post;
    }

    public Post Publish(DateTimeOffset publishedAt, string? coverImageUrl = null)
    {
        PublishedAt = Guard.NotDefault(publishedAt, nameof(publishedAt));
        CoverImageUrl = Guard.OptionalUrl(coverImageUrl, nameof(coverImageUrl));
        Status = PostStatus.Published;
        Touch(publishedAt);
        return this;
    }

    public Post Archive(DateTimeOffset archivedAt)
    {
        if (Status == PostStatus.Draft)
        {
            throw new DomainException("A draft post cannot be archived; publish or delete it instead.");
        }

        Status = PostStatus.Archived;
        Touch(archivedAt);
        return this;
    }

    public Post Edit(string title, string summary, string contentMarkdown, DateTimeOffset editedAt)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(title), 200);
        Summary = Guard.NotNullOrWhiteSpace(summary, nameof(summary), 500);
        ContentMarkdown = Guard.NotNullOrWhiteSpace(contentMarkdown, nameof(contentMarkdown), 200_000);
        Touch(editedAt);
        return this;
    }

    public Post ChangeSlug(Slug slug, DateTimeOffset changedAt)
    {
        Slug = Guard.NotNull(slug, nameof(slug));
        Touch(changedAt);
        return this;
    }

    /// <summary>Cheap counter used by the "trending" query on the home page.</summary>
    public void RegisterView()
    {
        if (ViewCount < int.MaxValue)
        {
            ViewCount++;
        }
    }

    public void AssignTag(Tag tag, DateTimeOffset assignedAt)
    {
        Guard.NotNull(tag, nameof(tag));

        // Equals, not ==: value objects are compared by value and this runs in memory (not in SQL).
        if (_postTags.Any(postTag => postTag.Tag.Slug.Equals(tag.Slug)))
        {
            return;
        }

        _postTags.Add(new PostTag(this, tag));
        Touch(assignedAt);
    }

    public void RemoveTag(Slug slug)
    {
        var existing = _postTags.FirstOrDefault(postTag => postTag.Tag.Slug.Equals(slug));

        if (existing is null)
        {
            return;
        }

        _postTags.Remove(existing);
    }

    private void Touch(DateTimeOffset moment)
    {
        UpdatedAt = Guard.NotDefault(moment, nameof(moment));
    }
}

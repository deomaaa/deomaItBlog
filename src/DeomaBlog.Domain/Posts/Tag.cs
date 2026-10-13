using DeomaBlog.Domain.Common;

namespace DeomaBlog.Domain.Posts;

/// <summary>
/// A label attached to posts ("csharp", "ef-core", "architecture").
/// Its slug is the identity used by everything user-visible.
/// </summary>
public sealed class Tag : Entity
{
    private readonly List<PostTag> _postTags = [];

    private Tag()
    {
        Name = string.Empty;
        Slug = Slug.Create("tag");
    }

    private Tag(string name, Slug slug)
    {
        Name = name;
        Slug = slug;
    }

    public string Name { get; private set; }

    public Slug Slug { get; private set; }

    public IReadOnlyCollection<PostTag> PostTags => _postTags.AsReadOnly();

    public static Tag Create(string name)
    {
        var clean = Guard.NotNullOrWhiteSpace(name, nameof(name), 64);
        return new Tag(clean, Slug.FromTitle(clean));
    }

    public static Tag Create(string name, string slug)
        => new(Guard.NotNullOrWhiteSpace(name, nameof(name), 64), Slug.Create(slug));

    public Tag Rename(string name)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), 64);
        return this;
    }

    public override string ToString() => Name;
}

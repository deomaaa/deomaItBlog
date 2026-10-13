namespace DeomaBlog.Domain.Posts;

/// <summary>
/// Explicit many-to-many link between <see cref="Post"/> and <see cref="Tag"/>.
/// Modelled as its own entity instead of an EF Core skip navigation so the join
/// table can grow columns later (ordering, "primary tag") without a breaking change.
/// </summary>
public sealed class PostTag
{
    private PostTag()
    {
        Post = null!;
        Tag = null!;
    }

    internal PostTag(Post post, Tag tag)
    {
        Post = post;
        Tag = tag;
        PostId = post.Id;
        TagId = tag.Id;
    }

    public Guid PostId { get; private set; }

    public Post Post { get; private set; }

    public Guid TagId { get; private set; }

    public Tag Tag { get; private set; }
}

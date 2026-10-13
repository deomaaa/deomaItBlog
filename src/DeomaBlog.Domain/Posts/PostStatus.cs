namespace DeomaBlog.Domain.Posts;

/// <summary>Lifecycle of a post inside the blog.</summary>
public enum PostStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

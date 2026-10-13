using DeomaBlog.Domain.Common;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.UnitTests.Domain;

public sealed class PostTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_post_is_a_draft_and_reads_its_slug_from_the_title()
    {
        var post = NewPost("Clean Architecture without the ceremony");

        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.Null(post.PublishedAt);
        Assert.False(post.IsPublished);
        Assert.Equal("clean-architecture-without-the-ceremony", post.Slug.Value);
    }

    [Theory]
    [InlineData("", "summary", "body")]
    [InlineData("title", "  ", "body")]
    [InlineData("title", "summary", "")]
    public void Create_rejects_empty_title_summary_or_body(string title, string summary, string body)
        => Assert.Throws<DomainException>(() => Post.Create(title, summary, body));

    [Fact]
    public void Publishing_sets_the_timestamp_and_makes_the_post_visible()
    {
        var post = NewPost("Publishing works");

        post.Publish(Now);

        Assert.True(post.IsPublished);
        Assert.Equal(Now, post.PublishedAt);
        Assert.Equal(Now, post.UpdatedAt);
    }

    [Fact]
    public void Publishing_with_a_default_timestamp_is_rejected()
        => Assert.Throws<DomainException>(() => NewPost("Nope").Publish(default));

    [Fact]
    public void A_draft_cannot_be_archived()
        => Assert.Throws<DomainException>(() => NewPost("Draft").Archive(Now));

    [Fact]
    public void Editing_updates_the_content_and_the_timestamp()
    {
        var post = NewPost("Original title");
        var edited = Now.AddHours(3);

        post.Publish(Now);
        post.Edit("New title", "New summary", "New body", edited);

        Assert.Equal("New title", post.Title);
        Assert.Equal("New body", post.ContentMarkdown);
        Assert.Equal(edited, post.UpdatedAt);
    }

    [Fact]
    public void Assigning_the_same_tag_twice_keeps_a_single_link()
    {
        var post = NewPost("Tagged post");
        var tag = Tag.Create("C#");

        post.AssignTag(tag, Now);
        post.AssignTag(tag, Now);

        Assert.Single(post.PostTags);
        Assert.Equal("c", post.PostTags.First().Tag.Slug.Value);
    }

    [Fact]
    public void Removing_a_tag_detaches_it()
    {
        var post = NewPost("Tagged post");
        var tag = Tag.Create("csharp");

        post.AssignTag(tag, Now);
        post.RemoveTag(tag.Slug);

        Assert.Empty(post.PostTags);
    }

    [Fact]
    public void Views_are_counted()
    {
        var post = NewPost("Counted");

        post.RegisterView();
        post.RegisterView();

        Assert.Equal(2, post.ViewCount);
    }

    [Fact]
    public void Reading_time_follows_the_body()
    {
        var post = NewPost("Long enough", string.Join(' ', Enumerable.Repeat("word", 360)));

        Assert.Equal(2, post.ReadingTime.Minutes);
    }

    private static Post NewPost(string title, string body = "Body of the post.")
        => Post.Create(title, "Summary of the post.", body);
}

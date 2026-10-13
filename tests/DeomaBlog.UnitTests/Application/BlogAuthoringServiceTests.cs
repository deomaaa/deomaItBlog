using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;
using DeomaBlog.Domain.Common;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.UnitTests.Application;

public sealed class BlogAuthoringServiceTests
{
    private readonly FakePostRepository _posts = new([]);
    private readonly FakeClock _clock = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task CreateAsync_builds_a_draft_without_publishing_it()
    {
        var created = await CreateService().CreateAsync(new CreatePostRequest("Draft post", "Summary", "Body"));

        Assert.Equal("draft-post", created.Slug);
        Assert.Null(created.PublishedAt);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_can_publish_immediately()
    {
        var created = await CreateService().CreateAsync(
            new CreatePostRequest("Live post", "Summary", "Body", PublishNow: true));

        Assert.Equal(_clock.UtcNow, created.PublishedAt);
    }

    [Fact]
    public async Task CreateAsync_assigns_the_requested_tags()
    {
        var created = await CreateService().CreateAsync(
            new CreatePostRequest("Tagged", "Summary", "Body", ["C#", "EF Core"]));

        Assert.Equal(2, created.Tags.Count);
    }

    [Fact]
    public async Task CreateAsync_appends_a_suffix_when_the_slug_is_taken()
    {
        await CreateService().CreateAsync(new CreatePostRequest("Same title", "Summary", "Body"));
        var second = await CreateService().CreateAsync(new CreatePostRequest("Same title", "Summary", "Body"));

        Assert.Equal("same-title-2", second.Slug);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_empty_title_through_the_domain_guard()
        => await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().CreateAsync(new CreatePostRequest("", "Summary", "Body")));

    [Fact]
    public async Task UpdateAsync_replaces_the_body_and_the_tag_set()
    {
        var created = await CreateService().CreateAsync(
            new CreatePostRequest("Editable", "Summary", "Body", ["old-tag"]));

        var updated = await CreateService().UpdateAsync(
            created.Slug,
            new UpdatePostRequest("Editable v2", "New summary", "New body", ["new-tag"]));

        Assert.Equal("Editable v2", updated.Title);
        Assert.Equal("New body", updated.ContentMarkdown);
        Assert.Equal("new-tag", Assert.Single(updated.Tags).Slug);
    }

    [Fact]
    public async Task PublishAsync_is_idempotent()
    {
        var created = await CreateService().CreateAsync(new CreatePostRequest("Publishable", "Summary", "Body"));
        var service = CreateService();

        var first = await service.PublishAsync(created.Slug);
        var second = await service.PublishAsync(created.Slug);

        Assert.Equal(first.PublishedAt, second.PublishedAt);
    }

    [Fact]
    public async Task ArchiveAsync_moves_a_published_post_out_of_the_feed()
    {
        var created = await CreateService().CreateAsync(
            new CreatePostRequest("Archivable", "Summary", "Body", PublishNow: true));

        await CreateService().ArchiveAsync(created.Slug);

        var feed = await _posts.GetPagedAsync(new PostFilter());
        Assert.Empty(feed.Items);
    }

    private BlogAuthoringService CreateService() => new(
        _posts,
        new FakeTagRepository([]),
        new FakeMarkdownRenderer(),
        _unitOfWork,
        _clock);
}

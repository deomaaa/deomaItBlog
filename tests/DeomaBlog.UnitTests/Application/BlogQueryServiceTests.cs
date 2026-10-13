using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Exceptions;
using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.UnitTests.Application;

/// <summary>
/// Verifies the use cases without a database: the ports of the Application layer are
/// implemented by hand here, which is the point of keeping them as interfaces.
/// </summary>
public sealed class BlogQueryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetPostsAsync_maps_the_page_and_keeps_paging_metadata()
    {
        var repository = new FakePostRepository(Enumerable.Range(1, 10)
            .Select(index => Published($"Post {index}", index))
            .ToList());

        var service = CreateService(repository);

        var page = await service.GetPostsAsync(new PostFilter { Page = 2, PageSize = 3 });

        Assert.Equal(3, page.Items.Count);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(4, page.TotalPages);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
        Assert.Equal("Post 4", page.Items[0].Title);
    }

    [Fact]
    public async Task GetPostsAsync_normalizes_an_absurd_page_size()
    {
        var repository = new FakePostRepository([Published("Only post", 1)]);
        var service = CreateService(repository);

        await service.GetPostsAsync(new PostFilter { Page = -3, PageSize = 1000 });

        Assert.Equal(1, repository.LastFilter!.Page);
        Assert.Equal(PostFilter.MaxPageSize, repository.LastFilter.PageSize);
    }

    [Fact]
    public async Task GetPostAsync_returns_the_rendered_html_and_the_raw_markdown()
    {
        var repository = new FakePostRepository([Published("Rendered", 1)]);
        var service = CreateService(repository);

        var post = await service.GetPostAsync("rendered");

        Assert.Equal("<p>Body of rendered.</p>", post.ContentHtml);
        Assert.Equal("Body of rendered.", post.ContentMarkdown);
        Assert.Equal("summary", post.Summary);
    }

    [Fact]
    public async Task GetPostAsync_throws_a_NotFoundException_for_an_unknown_slug()
    {
        var service = CreateService(new FakePostRepository([]));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPostAsync("does-not-exist"));
    }

    [Fact]
    public async Task GetTagsAsync_projects_tagnames_and_counts()
    {
        var tags = new FakeTagRepository([new TagWithCount("C#", "c", 4), new TagWithCount("EF Core", "ef-core", 2)]);
        var service = CreateService(new FakePostRepository([]), tags);

        var result = await service.GetTagsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("C#", result[0].Name);
        Assert.Equal(4, result[0].PostCount);
    }

    [Fact]
    public async Task RegisterViewAsync_saves_the_incremented_counter()
    {
        var post = Published("Viewed", 1);
        var repository = new FakePostRepository([post]);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork: unitOfWork);

        await service.RegisterViewAsync("viewed");

        Assert.Equal(1, post.ViewCount);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterViewAsync_ignores_an_unknown_slug()
    {
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakePostRepository([]), unitOfWork: unitOfWork);

        await service.RegisterViewAsync("ghost");

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static BlogQueryService CreateService(
        IPostRepository posts,
        ITagRepository? tags = null,
        IUnitOfWork? unitOfWork = null)
        => new(posts, tags ?? new FakeTagRepository([]), new FakeMarkdownRenderer(), unitOfWork ?? new FakeUnitOfWork());

    private static Post Published(string title, int daysAgo)
    {
        var post = Post.Create(title, "summary", $"Body of {title.ToLowerInvariant()}.");
        post.Publish(Now.AddDays(-daysAgo));
        return post;
    }
}

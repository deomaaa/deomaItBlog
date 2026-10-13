using System.Net;
using System.Net.Http.Json;
using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeomaBlog.IntegrationTests;

/// <summary>
/// Boots the real application (the same Program.cs used in production) against a throwaway
/// SQLite file: routing, model binding, EF Core, seeding and the API filters are all exercised.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"deomablog-tests-{Guid.CreateVersion7():N}.db");

    public const string AdminKey = "integration-test-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Admin:ApiKey", AdminKey);
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

public sealed class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_posts_returns_a_page_of_seeded_posts()
    {
        using var client = _factory.CreateApiClient();

        var page = await client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/posts?pageSize=3");

        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, post => Assert.False(string.IsNullOrWhiteSpace(post.Slug)));
        Assert.True(page.TotalCount >= page.Items.Count);
    }

    [Fact]
    public async Task Get_post_by_slug_returns_rendered_html()
    {
        using var client = _factory.CreateApiClient();
        var page = await client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/posts?pageSize=1");
        var slug = page!.Items[0].Slug;

        var post = await client.GetFromJsonAsync<PostDetailsDto>($"/api/posts/{slug}");

        Assert.NotNull(post);
        Assert.Contains("<", post!.ContentHtml, StringComparison.Ordinal);
        Assert.True(post.ReadingTimeMinutes >= 1);
    }

    [Fact]
    public async Task Get_unknown_post_returns_404_problem_details()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/posts/this-slug-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Tags_endpoint_reports_post_counts()
    {
        using var client = _factory.CreateApiClient();

        var tags = await client.GetFromJsonAsync<List<TagDto>>("/api/tags");

        Assert.NotNull(tags);
        Assert.NotEmpty(tags!);
        Assert.Contains(tags, tag => tag.PostCount > 0);
    }

    [Fact]
    public async Task Writing_without_the_api_key_is_unauthorized()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/posts", new CreatePostRequest("Nope", "Summary", "Body"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Writing_with_the_api_key_creates_a_post_that_can_be_read_back()
    {
        using var client = _factory.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/posts")
        {
            Content = JsonContent.Create(new CreatePostRequest(
                "Integration test post",
                "Written by the test suite.",
                "Body of the integration test post.",
                ["testing"],
                PublishNow: true)),
        };
        request.Headers.Add("X-Api-Key", ApiFactory.AdminKey);

        var response = await client.SendAsync(request);
        var created = await response.Content.ReadFromJsonAsync<PostDetailsDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);

        var readBack = await client.GetFromJsonAsync<PostDetailsDto>($"/api/posts/{created!.Slug}");
        Assert.Equal("Integration test post", readBack!.Title);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_openapi_document_is_published()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("openapi", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PageTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PageTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/posts")]
    [InlineData("/projects")]
    [InlineData("/work-experience")]
    [InlineData("/api/docs")]
    public async Task Pages_render_with_the_layout(string path)
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Demid Andreev", html, StringComparison.Ordinal);
        Assert.Contains("deoma", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_unknown_slug_renders_the_404_page()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/posts/not-a-real-post");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("404", html, StringComparison.Ordinal);
    }
}

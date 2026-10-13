using DeomaBlog.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeomaBlog.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies migrations and (on an empty database) inserts the demo content.
/// Idempotent: safe to run on every start-up, which is exactly what the systemd unit does.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly BlogDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(BlogDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        if (await _context.Posts.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already contains posts, skipping demo content.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var tags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
        var posts = new List<Post>();

        foreach (var seed in BlogSeedData.Posts)
        {
            var post = Post.Create(seed.Title, seed.Summary, seed.ContentMarkdown);
            post.Publish(now.AddDays(-seed.PublishedDaysAgo));

            for (var view = 0; view < seed.ViewCount; view++)
            {
                post.RegisterView();
            }

            foreach (var name in seed.Tags)
            {
                post.AssignTag(await ResolveTagAsync(name, tags, cancellationToken), now);
            }

            posts.Add(post);
        }

        _context.Posts.AddRange(posts);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded {PostCount} posts and {TagCount} tags.",
            posts.Count,
            tags.Count);
    }

    private async Task<Tag> ResolveTagAsync(
        string name,
        IDictionary<string, Tag> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var slug = Slug.FromTitle(name);
        var tag = await _context.Tags.FirstOrDefaultAsync(candidate => candidate.Slug == slug, cancellationToken);

        if (tag is null)
        {
            tag = Tag.Create(name);
            await _context.Tags.AddAsync(tag, cancellationToken);
        }

        cache[name] = tag;
        return tag;
    }
}

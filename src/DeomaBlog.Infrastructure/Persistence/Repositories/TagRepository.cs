using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace DeomaBlog.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ITagRepository" />
public sealed class TagRepository : ITagRepository
{
    private readonly BlogDbContext _context;

    public TagRepository(BlogDbContext context) => _context = context;

    public async Task<IReadOnlyList<TagWithCount>> GetWithPublishedCountsAsync(
        CancellationToken cancellationToken = default)
    {
        // Projected as the converted property (a column) and finished in memory: tag.Slug.Value
        // is not translatable, while tag.Slug is.
        var tags = await _context.Tags
            .AsNoTracking()
            .Select(tag => new
            {
                tag.Name,
                tag.Slug,
                PostCount = tag.PostTags.Count(postTag =>
                    postTag.Post.Status == PostStatus.Published && postTag.Post.PublishedAt != null),
            })
            .ToListAsync(cancellationToken);

        return tags
            .Select(tag => new TagWithCount(tag.Name, tag.Slug.Value, tag.PostCount))
            .OrderByDescending(tag => tag.PostCount)
            .ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<Tag?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
        => _context.Tags.FirstOrDefaultAsync(tag => tag.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Tag>> ResolveAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken = default)
    {
        var requested = names
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requested.Count == 0)
        {
            return [];
        }

        var slugs = requested.Select(Slug.FromTitle).ToList();
        var existing = await _context.Tags
            .Where(tag => slugs.Contains(tag.Slug))
            .ToListAsync(cancellationToken);

        var resolved = new List<Tag>(existing);
        var missing = requested
            .Where(name => existing.All(tag => !string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var name in missing)
        {
            var tag = Tag.Create(name);

            if (resolved.Any(existingTag => existingTag.Slug == tag.Slug))
            {
                continue;
            }

            await _context.Tags.AddAsync(tag, cancellationToken);
            resolved.Add(tag);
        }

        return resolved;
    }
}

using DeomaBlog.Domain.Posts;
using DeomaBlog.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;

namespace DeomaBlog.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work. The Application layer never references this type:
/// it talks to IPostRepository / ITagRepository / IUnitOfWork.
/// </summary>
public sealed class BlogDbContext : DbContext
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<PostTag> PostTags => Set<PostTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlogDbContext).Assembly);
        ApplySqliteFriendlyDateTimeConversion(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Applied as a convention rather than per entity: every timestamp in this model needs the
    /// same treatment, and forgetting one would show up only as a runtime query failure.
    /// </summary>
    private static void ApplySqliteFriendlyDateTimeConversion(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(type => type.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(DateTimeOffsetConverters.ToIsoUtc);
            }
        }
    }
}

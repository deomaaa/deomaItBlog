using DeomaBlog.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeomaBlog.Infrastructure.Persistence.Configurations;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");

        builder.HasKey(post => post.Id);
        builder.Property(post => post.Id).ValueGeneratedNever();

        builder.Property(post => post.Title).HasMaxLength(200).IsRequired();
        builder.Property(post => post.Summary).HasMaxLength(500).IsRequired();
        builder.Property(post => post.ContentMarkdown).IsRequired();
        builder.Property(post => post.CoverImageUrl).HasMaxLength(500);
        builder.Property(post => post.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(post => post.ViewCount).HasDefaultValue(0);
        builder.Property(post => post.CreatedAt).IsRequired();
        builder.Property(post => post.UpdatedAt);

        // Slug is a value object: persisted as its string form, rebuilt through its factory.
        builder.Property(post => post.Slug)
            .HasConversion(slug => slug.Value, value => Slug.Create(value))
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.HasIndex(post => post.Slug).IsUnique();
        builder.HasIndex(post => new { post.Status, post.PublishedAt });

        builder.HasMany(post => post.PostTags)
            .WithOne(postTag => postTag.Post)
            .HasForeignKey(postTag => postTag.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(post => post.PostTags)
            .HasField("_postTags")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedNever();

        builder.Property(tag => tag.Name).HasMaxLength(64).IsRequired();

        builder.Property(tag => tag.Slug)
            .HasConversion(slug => slug.Value, value => Slug.Create(value))
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.HasIndex(tag => tag.Slug).IsUnique();

        builder.Navigation(tag => tag.PostTags)
            .HasField("_postTags")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.ToTable("post_tags");

        builder.HasKey(postTag => new { postTag.PostId, postTag.TagId });

        builder.HasOne(postTag => postTag.Post)
            .WithMany(post => post.PostTags)
            .HasForeignKey(postTag => postTag.PostId);

        builder.HasOne(postTag => postTag.Tag)
            .WithMany(tag => tag.PostTags)
            .HasForeignKey(postTag => postTag.TagId);
    }
}

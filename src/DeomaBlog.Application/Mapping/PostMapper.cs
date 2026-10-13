using DeomaBlog.Application.Abstractions;
using DeomaBlog.Application.Abstractions.Persistence;
using DeomaBlog.Application.Models;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.Application.Mapping;

/// <summary>
/// Hand-written mapping instead of AutoMapper: it is explicit, allocation-cheap,
/// compile-time checked, and the mapping rules of a portfolio-sized project fit in one file.
/// </summary>
public static class PostMapper
{
    public static PostSummaryDto ToSummary(Post post) => new(
        post.Id,
        post.Title,
        post.Slug.Value,
        post.Summary,
        post.CoverImageUrl,
        post.PublishedAt,
        post.ReadingTime.Minutes,
        post.ViewCount,
        ToTags(post));

    public static PostDetailsDto ToDetails(Post post, IMarkdownRenderer markdown) => new(
        post.Id,
        post.Title,
        post.Slug.Value,
        post.Summary,
        markdown.ToHtml(post.ContentMarkdown),
        post.ContentMarkdown,
        post.CoverImageUrl,
        post.PublishedAt,
        post.UpdatedAt,
        post.ReadingTime.Minutes,
        post.ViewCount,
        ToTags(post));

    public static TagDto ToTag(TagWithCount tag) => new(tag.Name, tag.Slug, tag.PostCount);

    private static IReadOnlyList<TagDto> ToTags(Post post) => post.PostTags
        .Select(postTag => new TagDto(postTag.Tag.Name, postTag.Tag.Slug.Value, 0))
        .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

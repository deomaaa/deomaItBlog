namespace DeomaBlog.Application.Models;

/// <summary>Post representation used in lists, cards and API collections.</summary>
public sealed record PostSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string? CoverImageUrl,
    DateTimeOffset? PublishedAt,
    int ReadingTimeMinutes,
    int ViewCount,
    IReadOnlyList<TagDto> Tags)
{
    public string ReadingTimeDisplay => $"⏱ {ReadingTimeMinutes} min read";

    public string PublishedDisplay => PublishedAt?.ToString("dd MMM yyyy") ?? "unpublished";
}

/// <summary>Post representation used on the post page and by the details endpoint.</summary>
public sealed record PostDetailsDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string ContentHtml,
    string ContentMarkdown,
    string? CoverImageUrl,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UpdatedAt,
    int ReadingTimeMinutes,
    int ViewCount,
    IReadOnlyList<TagDto> Tags)
{
    public string PublishedDisplay => PublishedAt?.ToString("dd MMM yyyy") ?? "unpublished";

    public string ReadingTimeDisplay => $"⏱ {ReadingTimeMinutes} min read";

    public bool WasEdited => UpdatedAt is not null && PublishedAt is not null && UpdatedAt > PublishedAt;
}

/// <summary>Tag with the number of published posts carrying it.</summary>
public sealed record TagDto(string Name, string Slug, int PostCount);

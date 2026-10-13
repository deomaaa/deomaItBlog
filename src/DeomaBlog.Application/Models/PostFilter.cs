using DeomaBlog.Domain.Common;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.Application.Models;

/// <summary>Sorting options exposed by the REST API and used by the query service.</summary>
public enum PostSortOrder
{
    NewestFirst = 0,
    OldestFirst = 1,
    MostViewed = 2,
}

/// <summary>
/// Everything the query service needs to build a page of posts.
/// A single object keeps repository signatures stable when filters are added later.
/// </summary>
public sealed record PostFilter
{
    public const int MaxPageSize = 50;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 6;

    public string? Tag { get; init; }

    public string? Search { get; init; }

    public bool IncludeUnpublished { get; init; }

    public PostSortOrder Sort { get; init; } = PostSortOrder.NewestFirst;

    public PostFilter Normalized() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
        Tag = NormalizeTag(Tag),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
    };

    /// <summary>
    /// Tag filters arrive from the query string, so anything that is not a valid slug is mapped to
    /// an impossible-to-match value instead of throwing: a typo in the address bar should return
    /// "nothing found", not a 500.
    /// </summary>
    private static string? NormalizeTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return Slug.FromTitle(value).Value;
        }
        catch (DomainException)
        {
            return ImpossibleTag;
        }
    }

    internal const string ImpossibleTag = "no-such-tag-in-this-blog";

    public static PostFilter ForTag(Slug tag) => new() { Tag = tag.Value, PageSize = MaxPageSize };
}

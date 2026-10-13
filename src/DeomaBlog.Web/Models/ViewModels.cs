using DeomaBlog.Application.Models;
using DeomaBlog.Application.Posts;

namespace DeomaBlog.Web.Models;

/// <summary>View model of the landing page (about + latest posts + tag cloud).</summary>
public sealed record HomeViewModel(
    IReadOnlyList<PostSummaryDto> LatestPosts,
    IReadOnlyList<TagDto> Tags,
    int PostCount,
    int TotalReadingMinutes);

/// <summary>View model of the post list page, including the current filter state.</summary>
public sealed record PostListViewModel(
    PagedResult<PostSummaryDto> Posts,
    IReadOnlyList<TagDto> Tags,
    string? ActiveTag,
    string? Search,
    PostSortOrder Sort)
{
    public bool HasFilters => !string.IsNullOrWhiteSpace(ActiveTag) || !string.IsNullOrWhiteSpace(Search);

    public string SortToken => Sort switch
    {
        PostSortOrder.OldestFirst => "oldest",
        PostSortOrder.MostViewed => "popular",
        _ => "newest",
    };

    public string SortLabel => Sort switch
    {
        PostSortOrder.OldestFirst => "oldest first",
        PostSortOrder.MostViewed => "most read",
        _ => "newest first",
    };
}

/// <summary>View model of a single post page.</summary>
public sealed record PostPageViewModel(
    PostDetailsDto Post,
    IReadOnlyList<PostSummaryDto> Related);

/// <summary>Pagination state handed to the _Pager partial.</summary>
public sealed record PagerViewModel(int Page, int TotalPages, string? Tag, string? Search, string Sort)
{
    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>Builds "/posts?page=2&amp;tag=…" so the partial stays logic-free.</summary>
    public string LinkTo(int page)
    {
        var query = new List<string> { $"page={page}" };

        if (!string.IsNullOrWhiteSpace(Tag))
        {
            query.Add($"tag={Uri.EscapeDataString(Tag)}");
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query.Add($"q={Uri.EscapeDataString(Search)}");
        }

        if (Sort != "newest")
        {
            query.Add($"sort={Uri.EscapeDataString(Sort)}");
        }

        return $"/posts?{string.Join('&', query)}";
    }
}

/// <summary>A portfolio project card on /projects.</summary>
public sealed record ProjectCardViewModel(
    string Name,
    string Description,
    string Stack,
    string RepoUrl,
    string? LiveUrl = null);

/// <summary>One entry of the /work-experience timeline.</summary>
public sealed record TimelineEntryViewModel(
    string Period,
    string Title,
    string Place,
    IReadOnlyList<string> Highlights);

/// <summary>Model of the error page.</summary>
public sealed record ErrorViewModel(int StatusCode, string Title, string Message)
{
    public bool IsNotFound => StatusCode == StatusCodes.Status404NotFound;
}

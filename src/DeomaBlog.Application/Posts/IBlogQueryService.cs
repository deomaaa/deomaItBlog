using DeomaBlog.Application.Models;

namespace DeomaBlog.Application.Posts;

/// <summary>
/// Read side of the blog (the "Q" of CQRS, hand-rolled — no framework needed at this size).
/// Returns DTOs only, never domain entities, so callers cannot mutate an aggregate by accident.
/// </summary>
public interface IBlogQueryService
{
    Task<PagedResult<PostSummaryDto>> GetPostsAsync(PostFilter filter, CancellationToken cancellationToken = default);

    Task<PostDetailsDto> GetPostAsync(string slug, bool includeUnpublished = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostSummaryDto>> GetLatestAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostSummaryDto>> GetRelatedAsync(string slug, int count = 3, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>Increments the view counter and returns nothing — fire-and-forget analytics.</summary>
    Task RegisterViewAsync(string slug, CancellationToken cancellationToken = default);
}

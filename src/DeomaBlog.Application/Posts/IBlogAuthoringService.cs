using DeomaBlog.Application.Models;

namespace DeomaBlog.Application.Posts;

/// <summary>Payload for creating a post through the admin API.</summary>
public sealed record CreatePostRequest(
    string Title,
    string Summary,
    string ContentMarkdown,
    IReadOnlyList<string>? Tags = null,
    string? Slug = null,
    string? CoverImageUrl = null,
    bool PublishNow = false);

/// <summary>Payload for editing an existing post. Slugs are intentionally immutable.</summary>
public sealed record UpdatePostRequest(
    string Title,
    string Summary,
    string ContentMarkdown,
    IReadOnlyList<string>? Tags = null);

/// <summary>
/// Write side of the blog (the "C" of CQRS): create drafts, edit, publish, archive.
/// Every method returns the fresh DTO so HTTP callers never need a second round-trip.
/// </summary>
public interface IBlogAuthoringService
{
    Task<PostDetailsDto> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken = default);

    Task<PostDetailsDto> UpdateAsync(string slug, UpdatePostRequest request, CancellationToken cancellationToken = default);

    Task<PostDetailsDto> PublishAsync(string slug, CancellationToken cancellationToken = default);

    Task<PostDetailsDto> ArchiveAsync(string slug, CancellationToken cancellationToken = default);

    Task DeleteAsync(string slug, CancellationToken cancellationToken = default);
}

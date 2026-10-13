using DeomaBlog.Application.Models;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.Application.Abstractions.Persistence;

/// <summary>
/// Persistence port for the post aggregate. Implemented by EF Core in the
/// Infrastructure layer; the Application layer only sees this contract.
/// </summary>
public interface IPostRepository
{
    Task<PagedResult<Post>> GetPagedAsync(PostFilter filter, CancellationToken cancellationToken = default);

    Task<Post?> GetBySlugAsync(Slug slug, bool includeUnpublished = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Post>> GetLatestPublishedAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Posts sharing at least one tag with <paramref name="post"/>, newest first.</summary>
    Task<IReadOnlyList<Post>> GetRelatedAsync(Post post, int count, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(Slug slug, Guid? exceptPostId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Post post, CancellationToken cancellationToken = default);

    /// <summary>Detaches the aggregate; the actual DELETE happens on the next SaveChanges.</summary>
    void Remove(Post post);
}

/// <summary>Persistence port for tags.</summary>
public interface ITagRepository
{
    Task<IReadOnlyList<TagWithCount>> GetWithPublishedCountsAsync(CancellationToken cancellationToken = default);

    Task<Tag?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    /// <summary>Returns existing tags for the given names, creating the missing ones.</summary>
    Task<IReadOnlyList<Tag>> ResolveAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
}

/// <summary>Tag projected together with its usage count.</summary>
public sealed record TagWithCount(string Name, string Slug, int PostCount);

/// <summary>Transaction boundary (unit of work) over the underlying store.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

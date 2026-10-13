using DeomaBlog.Application.Abstractions.Persistence;

namespace DeomaBlog.Infrastructure.Persistence;

/// <inheritdoc cref="IUnitOfWork" />
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BlogDbContext _context;

    public UnitOfWork(BlogDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}

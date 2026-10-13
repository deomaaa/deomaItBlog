using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeomaBlog.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time, so migrations can be generated without
/// booting the web host. The connection string here is never used at runtime.
/// </summary>
internal sealed class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite("Data Source=blog.db")
            .Options;

        return new BlogDbContext(options);
    }
}

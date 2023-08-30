using blog_main.Models;
using Microsoft.EntityFrameworkCore;

namespace blog_main.Data 
{
    public class BlogDbContext : DbContext
    {
        public DbSet<Posts> Posts { get; set; }

        public BlogDbContext(DbContextOptions<BlogDbContext> options) 
            : base(options)
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }
    }
}
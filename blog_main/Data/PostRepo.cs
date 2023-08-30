namespace blog_main.Data
{
    public class PostRepo : IPostRepo
    {
        private readonly BlogDbContext _dbContext;
        public PostRepo(BlogDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
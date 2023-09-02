using Minio;
using Minio.AspNetCore;

namespace blog_main.Data
{
    public class PostRepo : IPostRepo
    {
        private readonly BlogDbContext _dbContext;
        private readonly IMinioClient _minioClient;
        public PostRepo(BlogDbContext dbContext, IMinioClientFactory minioClientFactory)
        {
            _dbContext = dbContext;
            _minioClient = minioClientFactory.CreateClient();
        }
    }
}
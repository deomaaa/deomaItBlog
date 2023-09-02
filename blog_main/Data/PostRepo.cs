using blog_main.Models;
using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<List<Post>> GetAllPosts()
        {
            var posts = await _dbContext.Posts.ToListAsync();
            
            return posts;
        }
    }
}
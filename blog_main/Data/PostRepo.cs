using blog_main.Models;
using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.AspNetCore;
using Minio.DataModel;
using Minio.Exceptions;

namespace blog_main.Data
{
    public class PostRepo : IPostRepo
    {
        readonly string CURRENT_DIR;
        private readonly BlogDbContext _dbContext;
        private readonly IMinioClient _minioClient;
        
        public PostRepo(BlogDbContext dbContext, IMinioClientFactory minioClientFactory)
        {
            _dbContext = dbContext;
            _minioClient = minioClientFactory.CreateClient();
            CURRENT_DIR = Directory.GetCurrentDirectory();
        }

        public async Task<List<Post>?> GetAllPosts()
        {
            try
            {
                var posts = await _dbContext.Posts.ToListAsync();

                return posts;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex);

                return null;
            }         
        }

        public async Task GetPostFileById(int post_id)
        {
            if(File.Exists($"{CURRENT_DIR}/wwwroot/post_files/{post_id}.cshtml"))
            {
                return;
            }
            else
            {
                try
                {
                    StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket("blog-posts-files")
                                                        .WithObject($"{post_id}.cshtml");
                    await _minioClient.StatObjectAsync(statObjectArgs);
                
                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                    .WithBucket("blog-posts-files")
                                                    .WithObject($"{post_id}.cshtml")
                                                    .WithFile($"{CURRENT_DIR}/wwwroot/post_files/{post_id}.cshtml");
                    await _minioClient.GetObjectAsync(getObjectArgs);
                }
                catch (MinioException e)
                {
                    Console.WriteLine("Error occurred: " + e);
                }
            }       
        }
    }
}
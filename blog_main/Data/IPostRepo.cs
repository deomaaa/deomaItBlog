using blog_main.Models;
using Microsoft.AspNetCore.Mvc;

namespace blog_main.Data
{
    public interface IPostRepo
    {
        Task<List<Post>> GetAllPosts();
    }
}
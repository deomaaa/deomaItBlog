using blog_main.Data;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.AspNetCore;

namespace blog_main.Controllers
{
    public class BlogController : Controller
    {
        private readonly IPostRepo _repository;

        public BlogController(IPostRepo repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Posts()
        {
            var posts = await _repository.GetAllPosts();

            return View(posts);
        }  
    }
}
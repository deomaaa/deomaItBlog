using blog_main.Data;
using Microsoft.AspNetCore.Mvc;

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
            return View();
        }  
    }
}
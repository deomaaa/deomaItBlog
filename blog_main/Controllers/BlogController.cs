using Microsoft.AspNetCore.Mvc;

namespace blog_main.Controllers
{
    public class BlogController : Controller
    {
        public BlogController()
        {

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
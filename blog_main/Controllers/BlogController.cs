using blog_main.Data;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.AspNetCore;

namespace blog_main.Controllers
{
    public class BlogController : Controller
    {
        private readonly IPostRepo _repository;
        private readonly IMinioClient _minioClient;

        public BlogController(IPostRepo repository, IMinioClientFactory minioClientFactory)
        {
            _repository = repository;
            _minioClient = minioClientFactory.CreateClient();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("{controller}/{action}/{bucketName?}")]
        public async Task<IActionResult> Posts(string bucketName)
        {
            var getListBucketsTask = await _minioClient.ListBucketsAsync().ConfigureAwait(false);

            // Iterate over the list of buckets.
            foreach (var bucket in getListBucketsTask.Buckets)
            {
                Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }
            return View();
        }  
    }
}
namespace blog_main.Dtos
{
    public class PostCreateDto
    {
        public string PostFileNameMinio { get;}
        public string PostName { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public int ViewsCount { get; set; } = 0;
        public DateTime DateOfCreation { get; set; } = DateTime.Now;
    }
}
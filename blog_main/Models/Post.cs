using System.ComponentModel.DataAnnotations;

namespace blog_main.Models
{
    public class Post
    {
        [Key]
        public int Post_Id { get; set;}
        public string PostFileNameMinio { get; set;}
        public string PostName { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public int ViewsCount { get; set; } = 0;
        public DateTime DateOfCreation { get; set; } = DateTime.Now;
    }
}
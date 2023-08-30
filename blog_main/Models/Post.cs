using System.ComponentModel.DataAnnotations;

namespace blog_main.Models
{
    public class Posts
    {
        [Key]
        public int Post_Id { get; set;}
        public string PostFileNameMinio { get;}
        public string PostName { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public int ViewsCount { get; set; }
        public DateTime DateOfCreation { get; set; }
    }
}
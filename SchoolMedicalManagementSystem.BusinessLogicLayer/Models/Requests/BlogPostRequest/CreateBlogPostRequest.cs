using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest
{
    public class CreateBlogPostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public Guid AuthorId { get; set; }
        public bool IsPublished { get; set; }
        public string CategoryName { get; set; }
        public bool? IsFeatured { get; set; }
    }
}

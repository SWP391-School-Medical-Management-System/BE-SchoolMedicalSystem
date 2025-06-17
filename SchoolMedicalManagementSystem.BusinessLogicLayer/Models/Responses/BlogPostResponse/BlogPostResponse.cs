using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BlogPostResponse
{
    public class BlogPostResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string AuthorName { get; set; }
        public bool IsPublished { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsFeatured { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest
{
    public class UpdateBlogPostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public bool? IsPublished { get; set; }
        public string CategoryName { get; set; }
        public bool? IsFeatured { get; set; }
    }
}

using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories
{
    public class BlogCommentRepository : BaseRepository<BlogComment>, IBlogCommentRepository
    {
        private readonly ApplicationDbContext _context;

        public BlogCommentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}

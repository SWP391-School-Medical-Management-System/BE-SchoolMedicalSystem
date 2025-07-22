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
    public class HealthCheckRepository : BaseRepository<HealthCheck>, IHealthCheckRepository
    {
        private readonly ApplicationDbContext _context;

        public HealthCheckRepository(ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }
    }
}

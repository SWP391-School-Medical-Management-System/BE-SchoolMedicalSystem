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
    public class VaccinationSessionRepository : BaseRepository<VaccinationSession>, IVaccinationSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public VaccinationSessionRepository(ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }
    }
}

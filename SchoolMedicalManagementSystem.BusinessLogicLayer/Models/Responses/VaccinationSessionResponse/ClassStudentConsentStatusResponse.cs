using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class ClassStudentConsentStatusResponse
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public int TotalStudents { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int DeclinedCount { get; set; }
        public List<StudentConsentStatusResponse> Students { get; set; }
    }
}

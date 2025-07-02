using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class MarkStudentVaccinatedRequest
    {
        public Guid StudentId { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public string NoteAfterSession { get; set; } = string.Empty;
    }
}

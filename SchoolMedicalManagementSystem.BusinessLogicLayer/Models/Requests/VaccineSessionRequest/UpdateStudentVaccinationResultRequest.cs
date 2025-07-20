using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class UpdateStudentVaccinationResultRequest
    {
        public Guid StudentId { get; set; }
        public bool IsVaccinated { get; set; }
        public string? NoteAfterSession { get; set; }
        public string? Symptoms { get; set; }
    }
}

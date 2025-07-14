using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class StudentVaccinationResultResponse
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public Guid VaccinationRecordId { get; set; }
        public Guid VaccinationTypeId { get; set; }
        public string VaccinationTypeName { get; set; }
        public int DoseNumber { get; set; }
        public DateTime? AdministeredDate { get; set; }
        public string AdministeredBy { get; set; }
        public string BatchNumber { get; set; }
        public string Notes { get; set; }
        public string VaccinationStatus { get; set; } // Scheduled, Completed, Missed, NotAdministered
        public string Symptoms { get; set; }
        public string NoteAfterSession { get; set; } 
        public string ClassName { get; set; } 
    }
}

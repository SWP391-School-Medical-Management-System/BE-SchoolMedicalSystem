using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class MarkStudentNotVaccinatedRequest
    {
        public Guid StudentId { get; set; }
        public string Reason { get; set; } // Lý do không tiêm
        public string? NoteAfterSession { get; set; } // Ghi chú bổ sung (tùy chọn)
    }
}

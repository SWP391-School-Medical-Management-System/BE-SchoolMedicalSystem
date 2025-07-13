using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class NurseAssignmentStatusResponse
    {
        public Guid NurseId { get; set; }
        public string NurseName { get; set; }
        public bool IsAssigned { get; set; }
        public List<Guid> AssignedClassIds { get; set; } = new List<Guid>();
        // Thay đổi từ string thành List<string>
        public List<string> AssignedClassNames { get; set; } = new List<string>();
    }
}

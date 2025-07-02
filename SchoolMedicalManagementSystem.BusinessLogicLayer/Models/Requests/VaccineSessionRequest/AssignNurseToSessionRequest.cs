using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class AssignNurseToSessionRequest
    {
        public Guid SessionId { get; set; }
        public List<ClassNurseAssignmentRequest> Assignments { get; set; } // Thêm danh sách phân công
    }

    public class ClassNurseAssignmentRequest
    {
        public Guid ClassId { get; set; }
        public Guid NurseId { get; set; }
    }
}

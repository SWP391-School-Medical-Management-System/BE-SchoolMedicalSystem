using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class ReAssignNurseToSessionRequest
    {
        public List<ClassNurseAssignmentRequest> Assignments { get; set; } // Thêm danh sách phân công
    }

}

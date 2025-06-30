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
        public Guid ClassId { get; set; }
        public Guid NurseId { get; set; }
    }
}

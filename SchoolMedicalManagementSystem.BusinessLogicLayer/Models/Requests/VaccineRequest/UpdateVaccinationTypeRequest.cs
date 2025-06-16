using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest
{
    public class UpdateVaccinationTypeRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int RecommendedAge { get; set; }
        public int DoseCount { get; set; }
    }
}

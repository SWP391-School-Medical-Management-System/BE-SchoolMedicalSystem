using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineResponse
{
    public class VaccinationTypeResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int RecommendedAge { get; set; }
        public int DoseCount { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}

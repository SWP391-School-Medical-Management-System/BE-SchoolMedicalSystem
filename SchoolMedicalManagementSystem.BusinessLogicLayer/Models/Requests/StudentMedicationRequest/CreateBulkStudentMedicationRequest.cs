using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest
{
    public class CreateBulkStudentMedicationRequest
    {
        public Guid StudentId { get; set; }
        public MedicationPriority Priority { get; set; } = MedicationPriority.Normal;
        public List<BulkMedicationDetails> Medications { get; set; } = new();

        public class BulkMedicationDetails
        {
            public string MedicationName { get; set; }
            public string Dosage { get; set; }
            public string Instructions { get; set; }
            public int? FrequencyCount { get; set; }
            public DateTime ExpiryDate { get; set; }
            public int QuantitySent { get; set; }
            public QuantityUnitEnum QuantityUnit { get; set; } 
            public string SpecialNotes { get; set; }
            public List<MedicationTimeOfDay> TimesOfDay { get; set; } = new();
            public DateTime? StartDate { get; set; } 
        }
    }
}

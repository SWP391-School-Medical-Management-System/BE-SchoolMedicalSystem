using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class UpdateMedicationStatusRequestValidator : AbstractValidator<UpdateMedicationStatusRequest>
{
    public UpdateMedicationStatusRequestValidator()
    {
        
    }
}
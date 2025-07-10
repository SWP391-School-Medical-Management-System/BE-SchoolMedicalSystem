using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class ApproveStudentMedicationRequestValidator : AbstractValidator<ApproveStudentMedicationRequest>
{
    public ApproveStudentMedicationRequestValidator()
    {
        RuleFor(x => x.IsApproved)
            .NotNull().WithMessage("Quyết định phê duyệt không được để trống.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
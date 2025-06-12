using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class ApproveStudentMedicationValidator : AbstractValidator<ApproveStudentMedicationRequest>
{
    public ApproveStudentMedicationValidator()
    {
        When(x => !x.IsApproved, () =>
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .WithMessage("Lý do từ chối không được để trống khi từ chối yêu cầu.")
                .MaximumLength(500)
                .WithMessage("Lý do từ chối không được vượt quá 500 ký tự.");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Ghi chú không được vượt quá 1000 ký tự.");
    }
}
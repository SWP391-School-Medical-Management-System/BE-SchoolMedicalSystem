using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class ApproveStudentMedicationRequestValidator : AbstractValidator<ApproveStudentMedicationRequest>
{
    public ApproveStudentMedicationRequestValidator()
    {
        RuleFor(x => x.IsApproved)
            .NotNull().WithMessage("Quyết định phê duyệt không được để trống.");

        When(x => x.IsApproved == false, () =>
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage("Lý do từ chối không được để trống khi từ chối yêu cầu.")
                .Length(10, 1000).WithMessage("Lý do từ chối phải từ 10 đến 1000 ký tự.");
        });

        When(x => x.IsApproved == true, () =>
        {
            RuleFor(x => x.RejectionReason)
                .Empty().WithMessage("Không được cung cấp lý do từ chối khi phê duyệt.");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
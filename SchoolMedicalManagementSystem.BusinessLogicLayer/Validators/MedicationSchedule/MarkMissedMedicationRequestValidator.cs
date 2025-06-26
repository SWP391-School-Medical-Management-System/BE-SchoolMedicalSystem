using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicationSchedule;

public class MarkMissedMedicationRequestValidator : AbstractValidator<MarkMissedMedicationRequest>
{
    public MarkMissedMedicationRequestValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty()
            .WithMessage("ID lịch trình không được để trống.");

        RuleFor(x => x.MissedReason)
            .NotEmpty()
            .WithMessage("Lý do bỏ lỡ không được để trống.")
            .MaximumLength(200)
            .WithMessage("Lý do bỏ lỡ không được vượt quá 200 ký tự.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
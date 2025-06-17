using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicationSchedule;

public class MarkStudentAbsentRequestValidator : AbstractValidator<MarkStudentAbsentRequest>
{
    public MarkStudentAbsentRequestValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty()
            .WithMessage("ID lịch trình không được để trống.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
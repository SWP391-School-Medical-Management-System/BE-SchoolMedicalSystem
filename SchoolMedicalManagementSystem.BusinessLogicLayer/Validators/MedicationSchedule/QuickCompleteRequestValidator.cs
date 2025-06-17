using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicationSchedule;

public class QuickCompleteRequestValidator : AbstractValidator<QuickCompleteRequest>
{
    public QuickCompleteRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(300)
            .WithMessage("Ghi chú không được vượt quá 300 ký tự.");

        RuleFor(x => x.Notes)
            .Must(notes => string.IsNullOrEmpty(notes) || notes.Trim().Length >= 5)
            .WithMessage("Ghi chú phải có ít nhất 5 ký tự có nghĩa.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Notes)
            .Must(notes => string.IsNullOrEmpty(notes) || !string.IsNullOrWhiteSpace(notes))
            .WithMessage("Ghi chú không được chỉ chứa khoảng trắng.");
    }
}
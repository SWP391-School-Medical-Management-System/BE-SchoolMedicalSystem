using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthEvent;

public class CompleteHealthEventValidator : AbstractValidator<CompleteHealthEventRequest>
{
    public CompleteHealthEventValidator()
    {
        RuleFor(x => x.ActionTaken)
            .NotEmpty()
            .WithMessage("Hành động đã thực hiện không được để trống.")
            .MaximumLength(1000)
            .WithMessage("Hành động đã thực hiện không được vượt quá 1000 ký tự.");

        RuleFor(x => x.Outcome)
            .NotEmpty()
            .WithMessage("Kết quả xử lý không được để trống.")
            .MaximumLength(1000)
            .WithMessage("Kết quả xử lý không được vượt quá 1000 ký tự.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
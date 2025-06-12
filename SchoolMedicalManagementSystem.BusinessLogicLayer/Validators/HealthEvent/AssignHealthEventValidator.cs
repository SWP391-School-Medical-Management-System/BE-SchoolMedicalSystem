using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthEvent;

public class AssignHealthEventValidator : AbstractValidator<AssignHealthEventRequest>
{
    public AssignHealthEventValidator()
    {
        RuleFor(x => x.NurseId)
            .NotEmpty()
            .WithMessage("ID y tá không được để trống.");
    }
}
using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.SchoolClass;

public class AddStudentToClassValidator : AbstractValidator<AddStudentToClassRequest>
{
    public AddStudentToClassValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("ID học sinh là bắt buộc.");
    }
}
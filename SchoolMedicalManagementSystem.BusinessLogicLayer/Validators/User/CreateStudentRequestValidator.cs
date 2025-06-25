using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class CreateStudentRequestValidator : BaseUserRequestValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage("Mã học sinh là bắt buộc")
            .MaximumLength(20).WithMessage("Mã học sinh không được vượt quá 20 ký tự");

        RuleFor(x => x.ParentId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("Mã phụ huynh cần được cung cấp.");

        RuleFor(x => x.ClassId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("Mã lớp cần được cung cấp.");
    }
}
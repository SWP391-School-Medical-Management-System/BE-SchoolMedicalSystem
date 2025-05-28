using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class CreateManagerRequestValidator : BaseUserRequestValidator<CreateManagerRequest>
{
    public CreateManagerRequestValidator()
    {
        RuleFor(x => x.StaffCode)
            .NotEmpty().WithMessage("Mã nhân viên là bắt buộc")
            .MaximumLength(20).WithMessage("Mã nhân viên không được vượt quá 20 ký tự");
    }
}
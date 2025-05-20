using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");
    }
}
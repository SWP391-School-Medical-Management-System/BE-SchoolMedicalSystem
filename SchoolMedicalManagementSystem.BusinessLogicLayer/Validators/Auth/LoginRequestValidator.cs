using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập/Email không được để trống")
            .MaximumLength(100).WithMessage("Tên đăng nhập/Email không được vượt quá 100 ký tự");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự");
    }
}
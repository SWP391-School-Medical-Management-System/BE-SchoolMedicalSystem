using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự")
            .MaximumLength(50).WithMessage("Mật khẩu mới không được vượt quá 50 ký tự")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống")
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu mới và xác nhận mật khẩu không khớp");
    }
}
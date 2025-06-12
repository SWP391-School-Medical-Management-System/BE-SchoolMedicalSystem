using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.Auth;

public class SetForgotPasswordRequestValidator : AbstractValidator<SetForgotPasswordRequest>
{
    public SetForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.OTP)
            .NotEmpty().WithMessage("OTP không được để trống.")
            .Length(6).WithMessage("OTP phải có đúng 6 chữ số.")
            .Matches(@"^\d{6}$").WithMessage("OTP chỉ được chứa số.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("Mật khẩu phải chứa ít nhất 1 chữ thường, 1 chữ hoa và 1 số.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
            .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp với mật khẩu mới.");
    }
}
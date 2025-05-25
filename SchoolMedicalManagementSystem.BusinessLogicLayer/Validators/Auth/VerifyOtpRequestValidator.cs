using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.Auth;

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP không được để trống.")
            .Length(6).WithMessage("OTP phải có đúng 6 chữ số.")
            .Matches(@"^\d{6}$").WithMessage("OTP chỉ được chứa số.");
    }
}
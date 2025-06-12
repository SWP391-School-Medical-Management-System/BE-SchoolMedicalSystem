using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public abstract class BaseUserRequestValidator<T> : AbstractValidator<T> where T : BaseUserRequest
{
    protected BaseUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống")
            .MinimumLength(3).WithMessage("Tên đăng nhập phải có ít nhất 3 ký tự")
            .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Tên đăng nhập chỉ được chứa chữ cái, số, dấu chấm, gạch dưới và gạch ngang");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống")
            .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$").WithMessage("Số điện thoại không hợp lệ");

        RuleFor(x => x.Address)
            .MaximumLength(255).WithMessage("Địa chỉ không được vượt quá 255 ký tự");

        RuleFor(x => x.Gender)
            .Must(g => string.IsNullOrEmpty(g) || g == "Male" || g == "Female" || g == "Other")
            .WithMessage("Giới tính phải là 'Male', 'Female', hoặc 'Other'");

        RuleFor(x => x.DateOfBirth)
            .Must(d => !d.HasValue || d.Value <= DateTime.Today)
            .WithMessage("Ngày sinh không được ở tương lai");
    }
}
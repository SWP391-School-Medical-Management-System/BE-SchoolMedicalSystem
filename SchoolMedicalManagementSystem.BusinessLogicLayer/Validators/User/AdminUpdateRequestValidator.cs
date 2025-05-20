using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class AdminUpdateUserRequestValidator : AbstractValidator<AdminUpdateUserRequest>
{
    public AdminUpdateUserRequestValidator()
    {
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

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Vai trò không được để trống")
            .Must(r => r == "MANAGER" || r == "SCHOOLNURSE")
            .WithMessage("Vai trò phải là 'MANAGER' hoặc 'SCHOOLNURSE'")
            .Must(r => r != "ADMIN")
            .WithMessage("Admin không thể cập nhật thành tài khoản Admin khác");

        When(x => x.Role == "SCHOOLNURSE", () =>
        {
            RuleFor(x => x.StaffCode)
                .NotEmpty().WithMessage("Mã nhân viên là bắt buộc cho y tá trường học")
                .MaximumLength(20).WithMessage("Mã nhân viên không được vượt quá 20 ký tự");

            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("Số giấy phép hành nghề là bắt buộc cho y tá trường học")
                .MaximumLength(50).WithMessage("Số giấy phép hành nghề không được vượt quá 50 ký tự");

            RuleFor(x => x.Specialization)
                .NotEmpty().WithMessage("Chuyên môn là bắt buộc cho y tá trường học")
                .MaximumLength(100).WithMessage("Chuyên môn không được vượt quá 100 ký tự");
        });

        When(x => x.Role == "MANAGER", () =>
        {
            RuleFor(x => x.StaffCode)
                .NotEmpty().WithMessage("Mã nhân viên là bắt buộc cho quản lý")
                .MaximumLength(20).WithMessage("Mã nhân viên không được vượt quá 20 ký tự");

            RuleFor(x => x.LicenseNumber)
                .Empty().WithMessage("Số giấy phép hành nghề không áp dụng cho vai trò Manager");

            RuleFor(x => x.Specialization)
                .Empty().WithMessage("Chuyên môn không áp dụng cho vai trò Manager");
        });
    }
}
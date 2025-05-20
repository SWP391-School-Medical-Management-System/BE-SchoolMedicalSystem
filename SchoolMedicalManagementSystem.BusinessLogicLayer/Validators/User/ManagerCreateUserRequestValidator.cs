using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class ManagerCreateUserRequestValidator : AbstractValidator<ManagerCreateUserRequest>
{
    public ManagerCreateUserRequestValidator()
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
        
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Vai trò không được để trống")
            .Must(r => r == "STUDENT" || r == "PARENT")
            .WithMessage("Vai trò phải là 'STUDENT' hoặc 'PARENT'")
            .Must(r => r != "ADMIN" && r != "MANAGER" && r != "SCHOOLNURSE")
            .WithMessage("Manager chỉ có thể tạo tài khoản Student hoặc Parent");

        When(x => x.Role == "STUDENT", () =>
        {
            RuleFor(x => x.StudentCode)
                .NotEmpty().WithMessage("Mã học sinh là bắt buộc cho học sinh")
                .MaximumLength(20).WithMessage("Mã học sinh không được vượt quá 20 ký tự");

            RuleFor(x => x.Relationship)
                .Empty().WithMessage("Mối quan hệ không áp dụng cho vai trò Student");
        });

        When(x => x.Role == "PARENT", () =>
        {
            RuleFor(x => x.Relationship)
                .NotEmpty().WithMessage("Mối quan hệ là bắt buộc cho phụ huynh")
                .Must(r => r == "Father" || r == "Mother" || r == "Guardian")
                .WithMessage("Mối quan hệ phải là 'Father', 'Mother', hoặc 'Guardian'");

            RuleFor(x => x.StudentCode)
                .Empty().WithMessage("Mã học sinh không áp dụng cho vai trò Parent");

            RuleFor(x => x.ClassId)
                .Null().WithMessage("ID lớp học không áp dụng cho vai trò Parent");

            RuleFor(x => x.ParentId)
                .Null().WithMessage("ID phụ huynh không áp dụng cho vai trò Parent");
        });
    }
}
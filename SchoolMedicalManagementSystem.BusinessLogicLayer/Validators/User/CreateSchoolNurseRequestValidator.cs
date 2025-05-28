using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class CreateSchoolNurseRequestValidator : BaseUserRequestValidator<CreateSchoolNurseRequest>
{
    public CreateSchoolNurseRequestValidator()
    {
        RuleFor(x => x.StaffCode)
            .NotEmpty().WithMessage("Mã nhân viên là bắt buộc")
            .MaximumLength(20).WithMessage("Mã nhân viên không được vượt quá 20 ký tự");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("Số giấy phép hành nghề là bắt buộc")
            .MaximumLength(50).WithMessage("Số giấy phép hành nghề không được vượt quá 50 ký tự");

        RuleFor(x => x.Specialization)
            .NotEmpty().WithMessage("Chuyên môn là bắt buộc")
            .MaximumLength(100).WithMessage("Chuyên môn không được vượt quá 100 ký tự");
    }
}
using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.SchoolClass;

public class CreateSchoolClassValidator : AbstractValidator<CreateSchoolClassRequest>
{
    public CreateSchoolClassValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tên lớp học là bắt buộc.")
            .Length(1, 20)
            .WithMessage("Tên lớp học phải từ 1 đến 20 ký tự.")
            .Matches(@"^[a-zA-Z0-9\s]+$")
            .WithMessage("Tên lớp học chỉ được chứa chữ cái, số và khoảng trắng.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(1, 12)
            .WithMessage("Khối lớp phải từ 1 đến 12.");

        RuleFor(x => x.AcademicYear)
            .InclusiveBetween(2020, 2030)
            .WithMessage("Năm học phải từ 2020 đến 2030.");
    }
}
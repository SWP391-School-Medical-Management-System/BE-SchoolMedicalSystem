using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.SchoolClass;

public class UpdateSchoolClassValidator : AbstractValidator<UpdateSchoolClassRequest>
{
    public UpdateSchoolClassValidator()
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
            .Must(BeValidAcademicYearForUpdate)
            .WithMessage($"Năm học phải trong khoảng {DateTime.Now.Year - 2} đến {DateTime.Now.Year + 2}.");
    }

    private bool BeValidAcademicYearForUpdate(int academicYear)
    {
        var currentYear = DateTime.Now.Year;
        return academicYear >= currentYear - 2 && academicYear <= currentYear + 2;
    }
}
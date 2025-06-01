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

        // cho phép tạo lớp cho năm trước (để import dữ liệu cũ)
        // và 2 năm tới (để chuẩn bị)
        RuleFor(x => x.AcademicYear)
            .Must(BeValidAcademicYearRange)
            .WithMessage($"Năm học phải trong khoảng {DateTime.Now.Year - 1} đến {DateTime.Now.Year + 2}.");
    }

    private bool BeValidAcademicYearRange(int academicYear)
    {
        var currentYear = DateTime.Now.Year;
        return academicYear >= currentYear - 1 && academicYear <= currentYear + 2;
    }
}
using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.SchoolClass;

public class AddStudentsToClassRequestValidator : AbstractValidator<AddStudentsToClassRequest>
{
    public AddStudentsToClassRequestValidator()
    {
        RuleFor(x => x.StudentIds)
            .NotNull()
            .WithMessage("Danh sách học sinh không được null.")
            .NotEmpty()
            .WithMessage("Danh sách học sinh không được để trống.")
            .Must(x => x.Count <= 50)
            .WithMessage("Chỉ được thêm tối đa 50 học sinh một lần.")
            .Must(x => x.All(id => id != Guid.Empty))
            .WithMessage("Tất cả StudentId phải hợp lệ.")
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("Không được có StudentId trùng lặp.");
    }
}
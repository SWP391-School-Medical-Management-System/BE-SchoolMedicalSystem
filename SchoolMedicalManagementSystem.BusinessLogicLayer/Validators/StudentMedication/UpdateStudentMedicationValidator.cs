using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class UpdateStudentMedicationValidator : AbstractValidator<UpdateStudentMedicationRequest>
{
    public UpdateStudentMedicationValidator()
    {
        When(x => !string.IsNullOrEmpty(x.MedicationName), () =>
        {
            RuleFor(x => x.MedicationName)
                .MaximumLength(200)
                .WithMessage("Tên thuốc không được vượt quá 200 ký tự.");
        });

        When(x => !string.IsNullOrEmpty(x.Dosage), () =>
        {
            RuleFor(x => x.Dosage)
                .MaximumLength(100)
                .WithMessage("Liều lượng không được vượt quá 100 ký tự.");
        });

        When(x => !string.IsNullOrEmpty(x.Instructions), () =>
        {
            RuleFor(x => x.Instructions)
                .MaximumLength(1000)
                .WithMessage("Hướng dẫn sử dụng không được vượt quá 1000 ký tự.");
        });

        When(x => x.StartDate.HasValue, () =>
        {
            RuleFor(x => x.StartDate)
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Ngày bắt đầu không được nhỏ hơn ngày hiện tại.");
        });

        When(x => x.EndDate.HasValue && x.StartDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        });

        When(x => x.ExpiryDate.HasValue, () =>
        {
            RuleFor(x => x.ExpiryDate)
                .GreaterThan(DateTime.Today)
                .WithMessage("Ngày hết hạn thuốc phải sau ngày hiện tại.");
        });
    }
}
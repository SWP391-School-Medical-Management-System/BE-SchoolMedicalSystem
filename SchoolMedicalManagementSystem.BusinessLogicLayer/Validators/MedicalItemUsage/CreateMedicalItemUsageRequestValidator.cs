using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalItemUsage;

public class CreateMedicalItemUsageRequestValidator : AbstractValidator<CreateMedicalItemUsageRequest>
{
    public CreateMedicalItemUsageRequestValidator()
    {
        RuleFor(x => x.MedicalItemId)
            .NotEmpty().WithMessage("ID vật phẩm y tế không được để trống.");

        RuleFor(x => x.HealthEventId)
            .NotEmpty().WithMessage("ID sự kiện y tế không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng sử dụng phải lớn hơn 0.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
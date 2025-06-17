using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class AddMoreMedicationValidator : AbstractValidator<AddMoreMedicationRequest>
{
    public AddMoreMedicationValidator()
    {
        RuleFor(x => x.StudentMedicationId)
            .NotEmpty()
            .WithMessage("ID thuốc học sinh không được để trống.");

        RuleFor(x => x.AdditionalQuantity)
            .GreaterThan(0)
            .WithMessage("Số lượng thêm phải lớn hơn 0.")
            .LessThanOrEqualTo(500)
            .WithMessage("Số lượng thêm không được vượt quá 500.");

        RuleFor(x => x.QuantityUnit)
            .NotEmpty()
            .WithMessage("Đơn vị không được để trống.")
            .Must(BeValidQuantityUnit)
            .WithMessage("Đơn vị không hợp lệ.");

        RuleFor(x => x.NewExpiryDate)
            .GreaterThan(DateTime.Today)
            .WithMessage("Ngày hết hạn phải sau ngày hiện tại.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }

    private bool BeValidQuantityUnit(string unit)
    {
        var validUnits = new[] { "viên", "chai", "gói", "túi", "ống", "tuýp", "lọ", "hộp" };
        return validUnits.Contains(unit.ToLower().Trim());
    }
}
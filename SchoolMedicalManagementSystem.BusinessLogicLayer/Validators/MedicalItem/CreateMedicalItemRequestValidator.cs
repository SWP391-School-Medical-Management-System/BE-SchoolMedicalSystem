using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalItem;

public class CreateMedicalItemRequestValidator : AbstractValidator<CreateMedicalItemRequest>
{
    public CreateMedicalItemRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loại vật phẩm y tế không được để trống.")
            .Must(type => type == "Medication" || type == "Supply")
            .WithMessage("Loại vật phẩm y tế phải là 'Medication' hoặc 'Supply'.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên vật phẩm y tế không được để trống.")
            .MaximumLength(200).WithMessage("Tên vật phẩm y tế không được vượt quá 200 ký tự.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả không được để trống.")
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.");

        RuleFor(x => x.Dosage)
            .MaximumLength(100).WithMessage("Liều lượng không được vượt quá 100 ký tự.")
            .Must((model, dosage) => model.Type != "Medication" || !string.IsNullOrEmpty(dosage))
            .WithMessage("Liều lượng là bắt buộc đối với thuốc.");

        RuleFor(x => x.Form)
            .Must((model, form) => model.Type != "Medication" || form.HasValue)
            .WithMessage("Dạng thuốc là bắt buộc đối với thuốc.")
            .Must(form => !form.HasValue ||
                          (form.HasValue && (form.Value == MedicationForm.Tablet
                                             || form.Value == MedicationForm.Syrup
                                             || form.Value == MedicationForm.Injection
                                             || form.Value == MedicationForm.Cream
                                             || form.Value == MedicationForm.Drops
                                             || form.Value == MedicationForm.Inhaler
                                             || form.Value == MedicationForm.Other)))
            .WithMessage("Dạng thuốc phải là Viên, Siro, Tiêm, Kem, Nhỏ giọt, Hít, hoặc Khác")
            .When(x => x.Form.HasValue);

        RuleFor(x => x.ExpiryDate)
            .Must(date => !date.HasValue || date.Value > DateTime.Now)
            .WithMessage("Ngày hết hạn phải lớn hơn ngày hiện tại.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Đơn vị không được để trống.")
            .MaximumLength(50).WithMessage("Đơn vị không được vượt quá 50 ký tự.");

        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("Lý do cần thêm thuốc/vật tư y tế không được để trống.")
            .MinimumLength(10).WithMessage("Lý do phải có ít nhất 10 ký tự.")
            .MaximumLength(1000).WithMessage("Lý do không được vượt quá 1000 ký tự.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Độ ưu tiên không hợp lệ.");

        RuleFor(x => x)
            .Must(HaveValidExpiryDateForMedication)
            .WithMessage("Thuốc phải có ngày hết hạn.")
            .When(x => x.Type == "Medication");

        RuleFor(x => x)
            .Must(x => !x.IsUrgent || x.Priority == PriorityLevel.High || x.Priority == PriorityLevel.Critical)
            .WithMessage("Yêu cầu khẩn cấp phải có độ ưu tiên Cao hoặc Khẩn cấp.");
    }

    private bool HaveValidExpiryDateForMedication(CreateMedicalItemRequest model)
    {
        if (model.Type != "Medication") return true;
        return model.ExpiryDate.HasValue;
    }
}
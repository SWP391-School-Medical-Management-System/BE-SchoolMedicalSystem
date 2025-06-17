using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicationSchedule;

public class BulkAdministerItemValidator : AbstractValidator<BulkAdministerItem>
{
    public BulkAdministerItemValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty()
            .WithMessage("ID lịch trình là bắt buộc.")
            .Must(id => id != Guid.Empty)
            .WithMessage("ID lịch trình phải là GUID hợp lệ.");

        RuleFor(x => x.ActualDosage)
            .NotEmpty()
            .WithMessage("Liều lượng thực tế là bắt buộc.")
            .MaximumLength(100)
            .WithMessage("Liều lượng thực tế không được vượt quá 100 ký tự.")
            .Matches(@"^[0-9]+(\.[0-9]+)?\s*(mg|ml|viên|gói|lần)$")
            .WithMessage("Liều lượng phải có định dạng hợp lệ (VD: 5mg, 2.5ml, 1 viên).");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Ghi chú không được vượt quá 500 ký tự.");

        RuleFor(x => x.RefusalReason)
            .NotEmpty()
            .WithMessage("Lý do từ chối là bắt buộc khi học sinh từ chối uống thuốc.")
            .MaximumLength(300)
            .WithMessage("Lý do từ chối không được vượt quá 300 ký tự.")
            .When(x => x.StudentRefused);

        RuleFor(x => x.SideEffectsObserved)
            .MaximumLength(500)
            .WithMessage("Mô tả tác dụng phụ không được vượt quá 500 ký tự.");

        RuleFor(x => x.SideEffectsObserved)
            .Must(sideEffects => !string.IsNullOrEmpty(sideEffects) && sideEffects.Trim().Length >= 10)
            .WithMessage("Khi quan sát thấy tác dụng phụ, cần mô tả chi tiết ít nhất 10 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.SideEffectsObserved));
    }
}
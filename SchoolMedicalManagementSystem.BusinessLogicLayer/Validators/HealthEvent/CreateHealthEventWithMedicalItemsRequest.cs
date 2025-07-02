using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthEvent;

public class CreateHealthEventWithMedicalItemsRequestValidator : AbstractValidator<CreateHealthEventWithMedicalItemsRequest>
{
    public CreateHealthEventWithMedicalItemsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID học sinh không được để trống.");

        RuleFor(x => x.EventType)
            .IsInEnum().WithMessage("Loại sự kiện y tế không hợp lệ.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả sự kiện không được để trống.")
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.");

        RuleFor(x => x.OccurredAt)
            .NotEmpty().WithMessage("Thời gian xảy ra không được để trống.")
            .LessThanOrEqualTo(DateTime.Now.AddHours(1)).WithMessage("Thời gian xảy ra không được trong tương lai.")
            .GreaterThan(DateTime.Now.AddDays(-90)).WithMessage("Thời gian xảy ra không được quá 90 ngày trước.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Địa điểm không được để trống.")
            .MaximumLength(200).WithMessage("Địa điểm không được vượt quá 200 ký tự.")
            .MinimumLength(2).WithMessage("Địa điểm phải có ít nhất 2 ký tự.");

        RuleFor(x => x.ActionTaken)
            .MaximumLength(1000).WithMessage("Hành động đã thực hiện không được vượt quá 1000 ký tự.")
            .MinimumLength(5).WithMessage("Hành động đã thực hiện phải có ít nhất 5 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.ActionTaken));

        RuleFor(x => x.Outcome)
            .MaximumLength(1000).WithMessage("Kết quả xử lý không được vượt quá 1000 ký tự.")
            .MinimumLength(5).WithMessage("Kết quả xử lý phải có ít nhất 5 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Outcome));

        RuleFor(x => x.CurrentHealthStatus)
            .NotEmpty().WithMessage("Tình trạng sức khỏe hiện tại không được để trống.")
            .MaximumLength(500).WithMessage("Tình trạng sức khỏe hiện tại không được vượt quá 500 ký tự.");

        RuleFor(x => x.ParentNotice)
            .MaximumLength(1000).WithMessage("Lưu ý về nhà không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.ParentNotice));

        RuleFor(x => x)
            .Must(ValidateEmergencyRequirements)
            .WithMessage("Sự kiện khẩn cấp phải có mô tả chi tiết và hành động xử lý cụ thể.")
            .When(x => x.IsEmergency);

        RuleFor(x => x)
            .Must(x => x.MedicalItemUsages.All(m => m.UsedAt >= x.OccurredAt && m.UsedAt <= DateTime.Now.AddHours(1)))
            .WithMessage("Thời gian sử dụng thuốc/vật tư phải sau thời gian xảy ra sự kiện và không được trong tương lai.")
            .When(x => x.MedicalItemUsages.Any());
    }

    private bool ValidateEmergencyRequirements(CreateHealthEventWithMedicalItemsRequest request)
    {
        if (!request.IsEmergency) return true;

        return !string.IsNullOrWhiteSpace(request.Description) &&
               request.Description.Length >= 20 &&
               !string.IsNullOrWhiteSpace(request.ActionTaken) &&
               request.ActionTaken.Length >= 10;
    }
}

public class MedicalItemUsageRequestValidator : AbstractValidator<MedicalItemUsageRequest>
{
    public MedicalItemUsageRequestValidator()
    {
        RuleFor(x => x.MedicalItemId)
            .NotEmpty().WithMessage("ID thuốc/vật tư không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.UsedAt)
            .NotEmpty()
            .WithMessage("Thời gian sử dụng không được để trống.")
            .LessThanOrEqualTo(DateTime.Now.AddHours(1))
            .WithMessage("Thời gian sử dụng không được trong tương lai.")
            .GreaterThan(DateTime.Now.AddDays(-90))
            .WithMessage("Thời gian sử dụng không được quá 90 ngày trước.");
    }
}
using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthEvent;

public class UpdateHealthEventRequestValidator : AbstractValidator<UpdateHealthEventRequest>
{
    public UpdateHealthEventRequestValidator()
    {
        RuleFor(x => x.EventType)
            .IsInEnum().WithMessage("Loại sự kiện y tế không hợp lệ.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả sự kiện không được để trống.")
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
            .MinimumLength(10).WithMessage("Mô tả phải có ít nhất 10 ký tự.");

        RuleFor(x => x.OccurredAt)
            .NotEmpty()
            .WithMessage("Thời gian xảy ra không được để trống.")
            .LessThanOrEqualTo(DateTime.Now.AddHours(1))
            .WithMessage("Thời gian xảy ra không được trong tương lai.")
            .GreaterThan(DateTime.Now.AddDays(-90))
            .WithMessage("Thời gian xảy ra không được quá 90 ngày trước.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Địa điểm không được để trống.")
            .MaximumLength(200)
            .WithMessage("Địa điểm không được vượt quá 200 ký tự.")
            .MinimumLength(2)
            .WithMessage("Địa điểm phải có ít nhất 2 ký tự.");

        RuleFor(x => x.ActionTaken)
            .MaximumLength(1000)
            .WithMessage("Hành động đã thực hiện không được vượt quá 1000 ký tự.")
            .MinimumLength(5)
            .WithMessage("Hành động đã thực hiện phải có ít nhất 5 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.ActionTaken));

        RuleFor(x => x.Outcome)
            .MaximumLength(1000)
            .WithMessage("Kết quả xử lý không được vượt quá 1000 ký tự.")
            .MinimumLength(5)
            .WithMessage("Kết quả xử lý phải có ít nhất 5 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Outcome));

        RuleFor(x => x)
            .Must(x => x.EventType != HealthEventType.AllergicReaction || x.RelatedMedicalConditionId.HasValue)
            .WithMessage("Sự kiện dị ứng phải liên kết với tình trạng y tế liên quan.")
            .WithName("RelatedMedicalConditionId");

        RuleFor(x => x)
            .Must(x => x.EventType != HealthEventType.ChronicIllnessEpisode || x.RelatedMedicalConditionId.HasValue)
            .WithMessage("Đợt tái phát bệnh mãn tính phải liên kết với tình trạng y tế liên quan.")
            .WithName("RelatedMedicalConditionId");
        
        RuleFor(x => x)
            .Must(ValidateEmergencyRequirements)
            .WithMessage("Sự kiện khẩn cấp phải có mô tả chi tiết (ít nhất 20 ký tự) và hành động xử lý cụ thể (ít nhất 10 ký tự).")
            .When(x => x.IsEmergency);
    }
    
    private bool ValidateEmergencyRequirements(UpdateHealthEventRequest request)
    {
        if (!request.IsEmergency) return true;

        return !string.IsNullOrWhiteSpace(request.Description) &&
               request.Description.Length >= 20 &&
               !string.IsNullOrWhiteSpace(request.ActionTaken) &&
               request.ActionTaken.Length >= 10;
    }
}
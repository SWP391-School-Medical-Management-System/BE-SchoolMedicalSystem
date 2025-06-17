using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class UpdateMedicationManagementValidator : AbstractValidator<UpdateMedicationManagementRequest>
{
    public UpdateMedicationManagementValidator()
    {
        RuleFor(x => x.TotalDoses)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalDoses.HasValue)
            .WithMessage("Tổng số liều phải >= 0.");

        RuleFor(x => x.RemainingDoses)
            .GreaterThanOrEqualTo(0)
            .When(x => x.RemainingDoses.HasValue)
            .WithMessage("Số liều còn lại phải >= 0.")
            .LessThanOrEqualTo(x => x.TotalDoses)
            .When(x => x.RemainingDoses.HasValue && x.TotalDoses.HasValue)
            .WithMessage("Số liều còn lại không được lớn hơn tổng số liều.");

        RuleFor(x => x.MinStockThreshold)
            .GreaterThanOrEqualTo(1)
            .When(x => x.MinStockThreshold.HasValue)
            .WithMessage("Ngưỡng cảnh báo phải >= 1.");

        RuleFor(x => x.ManagementNotes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.ManagementNotes))
            .WithMessage("Ghi chú quản lý không được vượt quá 1000 ký tự.");

        RuleFor(x => x.SpecificTimes)
            .Must(BeValidTimeJson)
            .When(x => !string.IsNullOrEmpty(x.SpecificTimes))
            .WithMessage("Định dạng thời gian không hợp lệ. Ví dụ: '[\"08:00\", \"12:00\"]'");
    }

    private bool BeValidTimeJson(string timeJson)
    {
        try
        {
            var times = System.Text.Json.JsonSerializer.Deserialize<List<string>>(timeJson);
            return times.All(t => TimeSpan.TryParse(t, out _));
        }
        catch
        {
            return false;
        }
    }
}
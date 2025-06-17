using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicationSchedule;

public class CreateMedicationScheduleRequestValidator : AbstractValidator<CreateMedicationScheduleRequest>
{
    public CreateMedicationScheduleRequestValidator()
    {
        RuleFor(x => x.StudentMedicationId)
            .NotEmpty()
            .WithMessage("ID thuốc học sinh không được để trống.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Ngày bắt đầu không được để trống.")
            .Must(BeValidStartDate)
            .WithMessage("Ngày bắt đầu không được quá 30 ngày trong quá khứ.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Ngày kết thúc không được để trống.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.")
            .Must((request, endDate) => (endDate - request.StartDate).TotalDays <= 365)
            .WithMessage("Thời gian điều trị không được quá 1 năm.");

        RuleFor(x => x.FrequencyType)
            .IsInEnum()
            .WithMessage("Loại tần suất không hợp lệ.");

        RuleFor(x => x.ScheduledTimes)
            .NotEmpty()
            .WithMessage("Phải có ít nhất một thời điểm uống thuốc.")
            .Must(HaveValidTimes)
            .WithMessage("Thời gian uống thuốc phải trong khoảng 6:00 - 20:00.");

        RuleFor(x => x.SpecificDays)
            .Must((request, specificDays) => ValidateSpecificDays(request.FrequencyType, specificDays))
            .WithMessage("Phải chỉ định các ngày cụ thể khi chọn tần suất 'Những ngày cụ thể'.");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("Hướng dẫn đặc biệt không được quá 500 ký tự.");
    }

    private bool BeValidStartDate(DateTime startDate)
    {
        return startDate >= DateTime.Today.AddDays(-30);
    }

    private bool HaveValidTimes(List<TimeSpan> times)
    {
        if (times == null || !times.Any()) return false;

        var schoolStart = new TimeSpan(6, 0, 0); // 6:00 AM
        var schoolEnd = new TimeSpan(20, 0, 0); // 8:00 PM

        return times.All(t => t >= schoolStart && t <= schoolEnd);
    }

    private bool ValidateSpecificDays(MedicationFrequencyType frequencyType, List<DayOfWeek>? specificDays)
    {
        if (frequencyType == MedicationFrequencyType.SpecificDays)
        {
            return specificDays != null && specificDays.Any();
        }

        return true;
    }
}
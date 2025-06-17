using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicationSchedule;

public class BulkAdministerRequestValidator : AbstractValidator<BulkAdministerRequest>
{
    public BulkAdministerRequestValidator()
    {
        RuleFor(x => x.Schedules)
            .NotNull()
            .WithMessage("Danh sách lịch trình không được null.")
            .NotEmpty()
            .WithMessage("Cần có ít nhất một lịch trình để xử lý.")
            .Must(schedules => schedules.Count <= 50)
            .WithMessage("Không thể xử lý quá 50 lịch trình cùng lúc.");

        // Validate từng item trong danh sách
        RuleForEach(x => x.Schedules)
            .SetValidator(new BulkAdministerItemValidator());

        // Kiểm tra không có ScheduleId trùng lặp
        RuleFor(x => x.Schedules)
            .Must(schedules => schedules.Select(s => s.ScheduleId).Distinct().Count() == schedules.Count)
            .WithMessage("Không được có lịch trình trùng lặp trong danh sách.");
    }
}
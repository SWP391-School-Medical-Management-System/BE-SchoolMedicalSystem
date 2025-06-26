using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class RejectStudentMedicationRequestValidator : AbstractValidator<RejectStudentMedicationRequest>
{
    public RejectStudentMedicationRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Lý do từ chối không được để trống.")
            .NotNull().WithMessage("Lý do từ chối không được null.")
            .Length(10, 1000).WithMessage("Lý do từ chối phải từ 10 đến 1000 ký tự.")
            .Must(BeValidRejectionReason).WithMessage("Lý do từ chối không hợp lệ.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
            .Must(BeValidNotes).WithMessage("Ghi chú chứa nội dung không phù hợp.");
    }

    private bool BeValidRejectionReason(string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return false;

        var invalidKeywords = new[] { "spam", "test", "abc", "123", "xxx" };
        return !invalidKeywords.Any(keyword =>
            rejectionReason.ToLower().Contains(keyword.ToLower()));
    }

    private bool BeValidNotes(string notes)
    {
        if (string.IsNullOrEmpty(notes))
            return true;

        if (notes.Trim().Length < 5)
            return false;

        var invalidKeywords = new[] { "spam", "test", "abc", "123" };
        return !invalidKeywords.Any(keyword =>
            notes.ToLower().Contains(keyword.ToLower()));
    }
}
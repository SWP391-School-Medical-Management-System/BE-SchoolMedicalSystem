using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalRecord;

public class UpdateMedicalRecordRequestValidator : AbstractValidator<UpdateMedicalRecordRequest>
{
    public UpdateMedicalRecordRequestValidator()
    {
        RuleFor(x => x.BloodType)
            .Must(BeValidBloodType)
            .WithMessage("Nhóm máu phải là: A, B, AB, O, A+, A-, B+, B-, AB+, AB-, O+, O-.")
            .When(x => !string.IsNullOrEmpty(x.BloodType));

        RuleFor(x => x.EmergencyContactPhone)
            .Matches(@"^[0-9+\-\s()]+$")
            .WithMessage("Số điện thoại không hợp lệ.")
            .MinimumLength(10)
            .WithMessage("Số điện thoại phải có ít nhất 10 số.")
            .MaximumLength(15)
            .WithMessage("Số điện thoại không được vượt quá 15 số.")
            .When(x => !string.IsNullOrEmpty(x.EmergencyContactPhone));

        // Validation to ensure at least one field is provided for update
        RuleFor(x => x)
            .Must(HaveAtLeastOneField)
            .WithMessage("Phải cung cấp ít nhất một trường để cập nhật.");
    }

    private bool BeValidBloodType(string bloodType)
    {
        if (string.IsNullOrEmpty(bloodType))
            return false;

        var validBloodTypes = new[] { "A", "B", "AB", "O", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        return validBloodTypes.Contains(bloodType.ToUpper());
    }

    private bool HaveAtLeastOneField(UpdateMedicalRecordRequest request)
    {
        return !string.IsNullOrEmpty(request.BloodType) ||
               !string.IsNullOrEmpty(request.EmergencyContact) ||
               !string.IsNullOrEmpty(request.EmergencyContactPhone);
    }
}
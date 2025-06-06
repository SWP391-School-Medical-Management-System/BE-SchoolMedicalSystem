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

        RuleFor(x => x.Height)
            .GreaterThan(0)
            .WithMessage("Chiều cao phải lớn hơn 0.")
            .LessThanOrEqualTo(250)
            .WithMessage("Chiều cao không thể vượt quá 250cm.")
            .When(x => x.Height.HasValue);

        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .WithMessage("Cân nặng phải lớn hơn 0.")
            .LessThanOrEqualTo(200)
            .WithMessage("Cân nặng không thể vượt quá 200kg.")
            .When(x => x.Weight.HasValue);

        RuleFor(x => x.EmergencyContact)
            .MaximumLength(100)
            .WithMessage("Tên người liên hệ khẩn cấp không được vượt quá 100 ký tự.")
            .Matches(@"^[a-zA-ZÀ-ỹ\s]+$")
            .WithMessage("Tên người liên hệ chỉ được chứa chữ cái và khoảng trắng.")
            .When(x => !string.IsNullOrEmpty(x.EmergencyContact));

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
               request.Height.HasValue ||
               request.Weight.HasValue ||
               !string.IsNullOrEmpty(request.EmergencyContact) ||
               !string.IsNullOrEmpty(request.EmergencyContactPhone);
    }
}
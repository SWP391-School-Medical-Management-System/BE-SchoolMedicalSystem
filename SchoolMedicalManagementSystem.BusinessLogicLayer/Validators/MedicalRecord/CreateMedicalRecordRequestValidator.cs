using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalRecord;

public class CreateMedicalRecordRequestValidator : AbstractValidator<CreateMedicalRecordRequest>
{
    public CreateMedicalRecordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID học sinh là bắt buộc.");

        RuleFor(x => x.BloodType)
            .NotEmpty()
            .WithMessage("Nhóm máu là bắt buộc.")
            .Must(BeValidBloodType)
            .WithMessage("Nhóm máu phải là: A, B, AB, O, A+, A-, B+, B-, AB+, AB-, O+, O-.");

        RuleFor(x => x.Height)
            .GreaterThan(0)
            .WithMessage("Chiều cao phải lớn hơn 0.")
            .LessThanOrEqualTo(250)
            .WithMessage("Chiều cao không thể vượt quá 250cm.");

        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .WithMessage("Cân nặng phải lớn hơn 0.")
            .LessThanOrEqualTo(200)
            .WithMessage("Cân nặng không thể vượt quá 200kg.");

        RuleFor(x => x.EmergencyContact)
            .NotEmpty()
            .WithMessage("Tên người liên hệ khẩn cấp là bắt buộc.")
            .MaximumLength(100)
            .WithMessage("Tên người liên hệ khẩn cấp không được vượt quá 100 ký tự.");

        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty()
            .WithMessage("Số điện thoại liên hệ khẩn cấp là bắt buộc.")
            .Matches(@"^[0-9+\-\s()]+$")
            .WithMessage("Số điện thoại không hợp lệ.")
            .MinimumLength(10)
            .WithMessage("Số điện thoại phải có ít nhất 10 số.")
            .MaximumLength(15)
            .WithMessage("Số điện thoại không được vượt quá 15 số.");
    }

    private bool BeValidBloodType(string bloodType)
    {
        if (string.IsNullOrEmpty(bloodType))
            return false;

        var validBloodTypes = new[] { "A", "B", "AB", "O", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        return validBloodTypes.Contains(bloodType.ToUpper());
    }
}
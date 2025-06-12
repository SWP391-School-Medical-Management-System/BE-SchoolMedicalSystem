using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalConditionRequest;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalCondition;

public class CreateMedicalConditionRequestValidator : AbstractValidator<CreateMedicalConditionRequest>
{
    // VALIDATION RULES SUMMARY:
    /*
    ALLERGY (Dị ứng):
    - Bắt buộc: medicalRecordId, type, name, reaction, severity
    - Khuyến nghị: treatment, medication, doctor, hospital
    - Không cần: diagnosisDate (vì dị ứng thường không có ngày chẩn đoán cụ thể)

    CHRONIC DISEASE (Bệnh mãn tính):
    - Bắt buộc: medicalRecordId, type, name, treatment, severity
    - Khuyến nghị: medication, reaction, diagnosisDate, doctor, hospital
    - Tùy chọn: notes

    MEDICAL HISTORY (Lịch sử y tế):
    - Bắt buộc: medicalRecordId, type, name, diagnosisDate, hospital
    - Khuyến nghị: treatment, medication, doctor
    - Tùy chọn: severity, reaction, notes
    */

    public CreateMedicalConditionRequestValidator()
    {
        RuleFor(x => x.MedicalRecordId)
            .NotEmpty()
            .WithMessage("ID hồ sơ y tế là bắt buộc.");

        RuleFor(x => x.Type)
            .Must(BeValidMedicalConditionType)
            .WithMessage(
                "Loại tình trạng y tế không hợp lệ. Các giá trị cho phép: Allergy (Dị ứng), ChronicDisease (Bệnh mãn tính), MedicalHistory (Lịch sử y tế).");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tên tình trạng y tế là bắt buộc.")
            .MaximumLength(200)
            .WithMessage("Tên tình trạng y tế không được vượt quá 200 ký tự.")
            .Matches(@"^[a-zA-ZÀ-ỹ0-9\s\-_.,()]+$")
            .WithMessage("Tên tình trạng y tế chứa ký tự không hợp lệ.");

        RuleFor(x => x.Severity)
            .Must(BeValidSeverityType)
            .WithMessage(
                "Mức độ nghiêm trọng không hợp lệ. Các giá trị cho phép: Mild (Nhẹ), Moderate (Trung bình), Severe (Nghiêm trọng).")
            .When(x => x.Severity.HasValue);

        RuleFor(x => x.Reaction)
            .MaximumLength(500)
            .WithMessage("Mô tả phản ứng không được vượt quá 500 ký tự.")
            .When(x => x.Reaction != null);

        RuleFor(x => x.Treatment)
            .MaximumLength(500)
            .WithMessage("Phương pháp điều trị không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Treatment));

        RuleFor(x => x.Medication)
            .MaximumLength(200)
            .WithMessage("Tên thuốc điều trị không được vượt quá 200 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Medication));

        RuleFor(x => x.DiagnosisDate)
            .LessThanOrEqualTo(DateTime.Now)
            .WithMessage("Ngày chẩn đoán không thể trong tương lai.")
            .GreaterThan(new DateTime(1900, 1, 1))
            .WithMessage("Ngày chẩn đoán không hợp lệ.")
            .When(x => x.DiagnosisDate.HasValue);

        RuleFor(x => x.Hospital)
            .MaximumLength(200)
            .WithMessage("Tên bệnh viện không được vượt quá 200 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Hospital));

        RuleFor(x => x.Doctor)
            .MaximumLength(100)
            .WithMessage("Tên bác sĩ không được vượt quá 100 ký tự.")
            .Matches(@"^[a-zA-ZÀ-ỹ\s.]+$")
            .WithMessage("Tên bác sĩ chỉ được chứa chữ cái, khoảng trắng và dấu chấm.")
            .When(x => !string.IsNullOrEmpty(x.Doctor));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        // Conditional validation based on MedicalConditionType
        RuleFor(x => x.Reaction)
            .NotEmpty()
            .WithMessage("Phản ứng dị ứng là bắt buộc đối với loại 'Allergy (Dị ứng)'.")
            .When(x => x.Type == MedicalConditionType.Allergy);

        RuleFor(x => x.Treatment)
            .NotEmpty()
            .WithMessage("Phương pháp điều trị là bắt buộc đối với loại 'ChronicDisease (Bệnh mãn tính)'.")
            .When(x => x.Type == MedicalConditionType.ChronicDisease);

        RuleFor(x => x.DiagnosisDate)
            .NotNull()
            .WithMessage("Ngày chẩn đoán là bắt buộc đối với loại 'MedicalHistory (Lịch sử y tế)'.")
            .When(x => x.Type == MedicalConditionType.MedicalHistory);

        RuleFor(x => x.Hospital)
            .NotEmpty()
            .WithMessage("Tên bệnh viện là bắt buộc đối với loại 'MedicalHistory (Lịch sử y tế)'.")
            .When(x => x.Type == MedicalConditionType.MedicalHistory);

        RuleFor(x => x.Severity)
            .NotNull()
            .WithMessage(
                "Mức độ nghiêm trọng là bắt buộc đối với 'Allergy (Dị ứng)' và 'ChronicDisease (Bệnh mãn tính)'. Các giá trị cho phép: Mild (Nhẹ), Moderate (Trung bình), Severe (Nghiêm trọng).")
            .When(x => x.Type == MedicalConditionType.Allergy || x.Type == MedicalConditionType.ChronicDisease);
    }

    private bool BeValidMedicalConditionType(MedicalConditionType type)
    {
        return Enum.IsDefined(typeof(MedicalConditionType), type);
    }

    private bool BeValidSeverityType(SeverityType? severity)
    {
        return !severity.HasValue || Enum.IsDefined(typeof(SeverityType), severity.Value);
    }
}
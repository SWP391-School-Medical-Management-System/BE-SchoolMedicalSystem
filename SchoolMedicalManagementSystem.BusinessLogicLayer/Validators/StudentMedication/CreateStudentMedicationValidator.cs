using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class CreateStudentMedicationValidator : AbstractValidator<CreateStudentMedicationRequest>
{
    public CreateStudentMedicationValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("ID học sinh không được để trống.");

        RuleFor(x => x.MedicationName)
            .NotEmpty()
            .WithMessage("Tên thuốc không được để trống.")
            .Length(2, 200)
            .WithMessage("Tên thuốc phải từ 2 đến 200 ký tự.")
            .Must(BeValidMedicationName)
            .WithMessage("Tên thuốc chứa ký tự không hợp lệ.");

        RuleFor(x => x.Dosage)
            .NotEmpty()
            .WithMessage("Liều lượng không được để trống.")
            .Length(1, 100)
            .WithMessage("Liều lượng phải từ 1 đến 100 ký tự.")
            .Must(BeValidDosage)
            .WithMessage("Liều lượng không đúng định dạng. Ví dụ: '1 viên', '5ml', '2 thìa'.");

        RuleFor(x => x.Instructions)
            .NotEmpty()
            .WithMessage("Hướng dẫn sử dụng không được để trống.")
            .Length(10, 1000)
            .WithMessage("Hướng dẫn sử dụng phải từ 10 đến 1000 ký tự.")
            .Must(BeValidInstructions)
            .WithMessage("Hướng dẫn sử dụng phải có nội dung có nghĩa.");

        RuleFor(x => x.Frequency)
            .NotEmpty()
            .WithMessage("Tần suất sử dụng không được để trống.")
            .Length(5, 200)
            .WithMessage("Tần suất sử dụng phải từ 5 đến 200 ký tự.")
            .Must(BeValidFrequency)
            .WithMessage("Tần suất uống không hợp lệ. Ví dụ: '2 lần/ngày', '3 lần/ngày sau ăn'.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Ngày bắt đầu không được để trống.")
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Ngày bắt đầu không được nhỏ hơn ngày hiện tại.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Ngày kết thúc không được để trống.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty()
            .WithMessage("Ngày hết hạn thuốc không được để trống.")
            .GreaterThan(DateTime.Today)
            .WithMessage("Thuốc đã hết hạn không thể gửi.")
            .GreaterThanOrEqualTo(x => x.EndDate)
            .WithMessage("Ngày hết hạn thuốc phải sau ngày kết thúc sử dụng.");

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .WithMessage("Mục đích sử dụng không được để trống.")
            .Length(10, 500)
            .WithMessage("Mục đích sử dụng phải từ 10 đến 500 ký tự.")
            .Must(BeValidPurpose)
            .WithMessage("Mục đích sử dụng phải có nội dung có nghĩa.");

        // Thông tin bác sĩ (không bắt buộc nhưng nếu có thì phải hợp lệ)
        RuleFor(x => x.DoctorName)
            .Length(2, 100)
            .When(x => !string.IsNullOrEmpty(x.DoctorName))
            .WithMessage("Tên bác sĩ phải từ 2 đến 100 ký tự.");

        RuleFor(x => x.Hospital)
            .Length(2, 200)
            .When(x => !string.IsNullOrEmpty(x.Hospital))
            .WithMessage("Tên bệnh viện/phòng khám phải từ 2 đến 200 ký tự.");

        RuleFor(x => x.PrescriptionDate)
            .LessThanOrEqualTo(DateTime.Today)
            .When(x => x.PrescriptionDate.HasValue)
            .WithMessage("Ngày kê đơn không được lớn hơn ngày hiện tại.");

        // Thông tin số lượng thuốc (BẮT BUỘC)
        RuleFor(x => x.QuantitySent)
            .GreaterThan(0)
            .WithMessage("Số lượng thuốc gửi phải lớn hơn 0.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Số lượng thuốc gửi không được vượt quá 1000.");

        RuleFor(x => x.QuantityUnit)
            .NotEmpty()
            .WithMessage("Đơn vị thuốc không được để trống.")
            .Must(BeValidQuantityUnit)
            .WithMessage("Đơn vị không hợp lệ. Ví dụ: 'viên', 'chai', 'gói', 'ống', 'tuýp'.");

        RuleFor(x => x.SideEffects)
            .MaximumLength(1000)
            .WithMessage("Tác dụng phụ không được vượt quá 1000 ký tự.");

        RuleFor(x => x.StorageInstructions)
            .MaximumLength(500)
            .WithMessage("Hướng dẫn bảo quản không được vượt quá 500 ký tự.");

        RuleFor(x => x.SpecialNotes)
            .MaximumLength(1000)
            .WithMessage("Ghi chú đặc biệt không được vượt quá 1000 ký tự.");

        RuleFor(x => x.EmergencyContactInstructions)
            .MaximumLength(500)
            .WithMessage("Hướng dẫn liên hệ khẩn cấp không được vượt quá 500 ký tự.");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Mức độ ưu tiên không hợp lệ.");

        RuleFor(x => x.TimeOfDay)
            .IsInEnum()
            .WithMessage("Thời điểm uống thuốc không hợp lệ.");

        RuleFor(x => x)
            .Must(HaveReasonableTreatmentDuration)
            .WithMessage("Thời gian điều trị phải ít nhất 1 ngày và không quá 1 năm.")
            .WithName("TreatmentDuration");
    }

    private bool BeValidMedicationName(string medicationName)
    {
        if (string.IsNullOrWhiteSpace(medicationName))
            return false;

        var pattern = @"^[a-zA-ZÀ-ỹ0-9\s\-\.\(\)]+$";
        var isValidPattern = System.Text.RegularExpressions.Regex.IsMatch(medicationName, pattern);

        var invalidKeywords = new[] { "test", "abc", "xxx", "123456" };
        var hasInvalidKeywords = invalidKeywords.Any(keyword =>
            medicationName.ToLower().Contains(keyword));

        return isValidPattern && !hasInvalidKeywords;
    }

    private bool BeValidDosage(string dosage)
    {
        if (string.IsNullOrWhiteSpace(dosage))
            return false;

        var dosagePattern = @"^[\d\.\,]+\s*(viên|ml|mg|g|lần|giọt|thìa|túi|gói|cap|capsule|tablet)(\s+.*)?$";
        return System.Text.RegularExpressions.Regex.IsMatch(dosage.Trim(), dosagePattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private bool BeValidInstructions(string instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
            return false;

        var meaningfulWords = new[]
        {
            "uống", "ăn", "trước", "sau", "sáng", "trưa", "chiều", "tối",
            "ngày", "giờ", "phút", "nước", "thức ăn", "khi", "nếu", "theo"
        };

        return meaningfulWords.Any(word =>
            instructions.ToLower().Contains(word));
    }

    private bool BeValidFrequency(string frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
            return false;

        var frequencyPatterns = new[]
        {
            @"\d+\s*lần\s*/\s*ngày",
            @"\d+\s*lần.*ngày",
            @"mỗi\s*\d+\s*giờ",
            @"\d+\s*giờ\s*/\s*lần",
            @"(sáng|trưa|chiều|tối)",
            @"khi\s*(cần|đau|sốt)"
        };

        return frequencyPatterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(frequency.ToLower(), pattern));
    }

    private bool BeValidPurpose(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            return false;

        var medicalKeywords = new[]
        {
            "điều trị", "chữa", "giảm", "ngăn ngừa", "phòng", "hỗ trợ",
            "đau", "sốt", "ho", "cảm", "dị ứng", "viêm", "nhiễm trùng",
            "tiêu hóa", "tim", "phổi", "da", "mắt", "tai", "mũi", "họng",
            "vitamin", "canxi", "sắt", "kẽm", "bổ sung"
        };

        return medicalKeywords.Any(keyword =>
            purpose.ToLower().Contains(keyword));
    }

    private bool BeValidQuantityUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            return false;

        var validUnits = new[]
        {
            "viên", "chai", "gói", "túi", "ống", "tuýp", "lọ", "hộp",
            "vỉ", "strip", "cap", "capsule", "tablet", "ml"
        };

        return validUnits.Any(validUnit =>
            unit.ToLower().Trim().Equals(validUnit, StringComparison.OrdinalIgnoreCase));
    }

    private bool HaveReasonableTreatmentDuration(CreateStudentMedicationRequest request)
    {
        var duration = request.EndDate - request.StartDate;
        return duration.TotalDays >= 1 && duration.TotalDays <= 365;
    }
}
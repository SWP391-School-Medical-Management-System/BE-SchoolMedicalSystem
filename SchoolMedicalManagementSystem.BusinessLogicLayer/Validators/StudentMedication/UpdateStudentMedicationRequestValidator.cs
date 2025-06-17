using FluentValidation;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class UpdateStudentMedicationRequestValidator : AbstractValidator<UpdateStudentMedicationRequest>
{
    public UpdateStudentMedicationRequestValidator()
    {
        RuleFor(x => x.MedicationName)
            .Length(2, 200).When(x => !string.IsNullOrEmpty(x.MedicationName))
            .WithMessage("Tên thuốc phải từ 2 đến 200 ký tự.")
            .Must(BeValidMedicationName).When(x => !string.IsNullOrEmpty(x.MedicationName))
            .WithMessage("Tên thuốc chứa ký tự không hợp lệ.");

        RuleFor(x => x.Dosage)
            .Length(1, 100).When(x => !string.IsNullOrEmpty(x.Dosage))
            .WithMessage("Liều lượng phải từ 1 đến 100 ký tự.")
            .Must(BeValidDosage).When(x => !string.IsNullOrEmpty(x.Dosage))
            .WithMessage("Liều lượng không đúng định dạng. Ví dụ: '1 viên', '5ml', '2 thìa'.");

        RuleFor(x => x.Instructions)
            .Length(10, 1000).When(x => !string.IsNullOrEmpty(x.Instructions))
            .WithMessage("Hướng dẫn sử dụng phải từ 10 đến 1000 ký tự.")
            .Must(BeValidInstructions).When(x => !string.IsNullOrEmpty(x.Instructions))
            .WithMessage("Hướng dẫn sử dụng phải có nội dung có nghĩa.");

        RuleFor(x => x.Frequency)
            .Length(5, 100).When(x => !string.IsNullOrEmpty(x.Frequency))
            .WithMessage("Tần suất uống phải từ 5 đến 100 ký tự.")
            .Must(BeValidFrequency).When(x => !string.IsNullOrEmpty(x.Frequency))
            .WithMessage("Tần suất uống không hợp lệ. Ví dụ: '2 lần/ngày', '3 lần/ngày sau ăn'.");

        RuleFor(x => x.Purpose)
            .Length(10, 500).When(x => !string.IsNullOrEmpty(x.Purpose))
            .WithMessage("Mục đích sử dụng phải từ 10 đến 500 ký tự.")
            .Must(BeValidPurpose).When(x => !string.IsNullOrEmpty(x.Purpose))
            .WithMessage("Mục đích sử dụng phải có nội dung có nghĩa.");

        RuleFor(x => x.DoctorName)
            .Length(2, 100).When(x => !string.IsNullOrEmpty(x.DoctorName))
            .WithMessage("Tên bác sĩ phải từ 2 đến 100 ký tự.")
            .Must(BeValidPersonName).When(x => !string.IsNullOrEmpty(x.DoctorName))
            .WithMessage("Tên bác sĩ chứa ký tự không hợp lệ.");

        RuleFor(x => x.Hospital)
            .Length(5, 200).When(x => !string.IsNullOrEmpty(x.Hospital))
            .WithMessage("Tên bệnh viện phải từ 5 đến 200 ký tự.")
            .Must(BeValidHospitalName).When(x => !string.IsNullOrEmpty(x.Hospital))
            .WithMessage("Tên bệnh viện chứa ký tự không hợp lệ.");

        RuleFor(x => x.PrescriptionNumber)
            .Length(1, 50).When(x => !string.IsNullOrEmpty(x.PrescriptionNumber))
            .WithMessage("Số đơn thuốc phải từ 1 đến 50 ký tự.")
            .Must(BeValidPrescriptionNumber).When(x => !string.IsNullOrEmpty(x.PrescriptionNumber))
            .WithMessage("Số đơn thuốc chỉ được chứa chữ, số và ký tự đặc biệt cơ bản.");

        RuleFor(x => x.QuantityUnit)
            .Length(1, 20).When(x => !string.IsNullOrEmpty(x.QuantityUnit))
            .WithMessage("Đơn vị thuốc phải từ 1 đến 20 ký tự.")
            .Must(BeValidQuantityUnit).When(x => !string.IsNullOrEmpty(x.QuantityUnit))
            .WithMessage("Đơn vị thuốc không hợp lệ. Ví dụ: 'viên', 'ml', 'gói', 'thìa'.");

        RuleFor(x => x.SideEffects)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.SideEffects))
            .WithMessage("Tác dụng phụ không được vượt quá 1000 ký tự.");

        RuleFor(x => x.StorageInstructions)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.StorageInstructions))
            .WithMessage("Hướng dẫn bảo quản không được vượt quá 500 ký tự.");

        RuleFor(x => x.SpecialNotes)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.SpecialNotes))
            .WithMessage("Ghi chú đặc biệt không được vượt quá 1000 ký tự.");

        RuleFor(x => x.EmergencyContactInstructions)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.EmergencyContactInstructions))
            .WithMessage("Hướng dẫn liên hệ khẩn cấp không được vượt quá 500 ký tự.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.Today).When(x => x.StartDate.HasValue)
            .WithMessage("Ngày bắt đầu phải từ hôm nay trở đi.");

        RuleFor(x => x.EndDate)
            .GreaterThan(DateTime.Today).When(x => x.EndDate.HasValue)
            .WithMessage("Ngày kết thúc phải sau hôm nay.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.Today).When(x => x.ExpiryDate.HasValue)
            .WithMessage("Ngày hết hạn phải sau hôm nay.");

        RuleFor(x => x.PrescriptionDate)
            .LessThanOrEqualTo(DateTime.Today).When(x => x.PrescriptionDate.HasValue)
            .WithMessage("Ngày kê đơn không được sau hôm nay.")
            .GreaterThanOrEqualTo(DateTime.Today.AddYears(-1)).When(x => x.PrescriptionDate.HasValue)
            .WithMessage("Ngày kê đơn không được quá 1 năm trước.");

        RuleFor(x => x.QuantitySent)
            .GreaterThan(0).When(x => x.QuantitySent.HasValue)
            .WithMessage("Số lượng thuốc gửi phải lớn hơn 0.")
            .LessThanOrEqualTo(1000).When(x => x.QuantitySent.HasValue)
            .WithMessage("Số lượng thuốc gửi không được vượt quá 1000.");

        RuleFor(x => x.Priority)
            .IsInEnum().When(x => x.Priority.HasValue)
            .WithMessage("Mức độ ưu tiên không hợp lệ.");

        RuleFor(x => x.TimeOfDay)
            .IsInEnum().When(x => x.TimeOfDay.HasValue)
            .WithMessage("Thời điểm uống thuốc không hợp lệ.");

        RuleFor(x => x)
            .Must(HaveValidDateRange)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.")
            .WithName("DateRange");

        RuleFor(x => x)
            .Must(HaveValidExpiryDate)
            .WithMessage("Ngày hết hạn phải sau ngày kết thúc.")
            .WithName("ExpiryDate");

        RuleFor(x => x)
            .Must(HaveAtLeastOneField)
            .WithMessage("Phải cung cấp ít nhất một trường để cập nhật.")
            .WithName("UpdateFields");

        RuleFor(x => x)
            .Must(HaveReasonableUpdateWindow)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Thời gian sử dụng thuốc phải ít nhất 1 ngày và không quá 1 năm.")
            .WithName("TreatmentDuration");

        RuleFor(x => x)
            .Must(HaveValidPrescriptionDateRange)
            .When(x => x.PrescriptionDate.HasValue && x.StartDate.HasValue)
            .WithMessage("Ngày kê đơn phải trước hoặc bằng ngày bắt đầu uống thuốc.")
            .WithName("PrescriptionDateRange");
    }

    #region Validation Helper Methods

    private bool BeValidMedicationName(string medicationName)
    {
        if (string.IsNullOrWhiteSpace(medicationName))
            return true;

        var pattern = @"^[a-zA-ZÀ-ỹ0-9\s\-\.\(\)\/\+]+$";
        var isValidPattern = System.Text.RegularExpressions.Regex.IsMatch(medicationName, pattern);

        var invalidKeywords = new[] { "test", "abc", "xxx", "123456", "aaaa", "bbbb" };
        var hasInvalidKeywords = invalidKeywords.Any(keyword =>
            medicationName.ToLower().Contains(keyword));

        return isValidPattern && !hasInvalidKeywords;
    }

    private bool BeValidDosage(string dosage)
    {
        if (string.IsNullOrWhiteSpace(dosage))
            return true;

        // Updated pattern to match Vietnamese medication units
        var dosagePattern =
            @"^[\d\.\,\s]*\s*(viên|ml|mg|g|lần|giọt|thìa|túi|gói|cap|capsule|tablet|mcg|µg|IU|unit)(\s+.*)?$";
        return System.Text.RegularExpressions.Regex.IsMatch(dosage.Trim(), dosagePattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private bool BeValidInstructions(string instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
            return true;

        var meaningfulWords = new[]
        {
            "uống", "ăn", "trước", "sau", "sáng", "trưa", "chiều", "tối",
            "ngày", "giờ", "phút", "nước", "thức ăn", "khi", "nếu", "theo",
            "bữa", "đói", "no", "cần", "đau", "sốt", "ho", "ngủ"
        };

        return meaningfulWords.Any(word =>
            instructions.ToLower().Contains(word));
    }

    private bool BeValidFrequency(string frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
            return true;

        var frequencyPatterns = new[]
        {
            @"\d+\s*lần\s*/\s*ngày",
            @"\d+\s*lần.*ngày",
            @"mỗi\s*\d+\s*giờ",
            @"\d+\s*giờ\s*/\s*lần",
            @"(sáng|trưa|chiều|tối|đêm)",
            @"khi\s*(cần|đau|sốt|ho)",
            @"trước.*bữa",
            @"sau.*bữa"
        };

        return frequencyPatterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(frequency.ToLower(), pattern));
    }

    private bool BeValidPurpose(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            return true;

        var medicalKeywords = new[]
        {
            "điều trị", "chữa", "giảm", "ngăn ngừa", "phòng", "hỗ trợ",
            "đau", "sốt", "ho", "cảm", "dị ứng", "viêm", "nhiễm trùng",
            "tiêu hóa", "tim", "phổi", "da", "mắt", "tai", "mũi", "họng",
            "vitamin", "canxi", "sắt", "kẽm", "bổ sung", "huyết áp",
            "tiểu đường", "hen", "co giật", "động kinh", "đái", "táo bón"
        };

        return medicalKeywords.Any(keyword =>
            purpose.ToLower().Contains(keyword));
    }

    private bool BeValidPersonName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        var namePattern = @"^[a-zA-ZÀ-ỹ\s\.\-]+$";
        return System.Text.RegularExpressions.Regex.IsMatch(name, namePattern);
    }

    private bool BeValidHospitalName(string hospitalName)
    {
        if (string.IsNullOrWhiteSpace(hospitalName))
            return true;

        var hospitalPattern = @"^[a-zA-ZÀ-ỹ0-9\s\-\.\(\)\/]+$";
        var isValidPattern = System.Text.RegularExpressions.Regex.IsMatch(hospitalName, hospitalPattern);

        var medicalKeywords = new[]
        {
            "bệnh viện", "phòng khám", "trung tâm", "y tế", "đa khoa",
            "chuyên khoa", "nhi", "sản", "mắt", "răng", "hospital", "clinic"
        };

        var hasMedicalKeyword = medicalKeywords.Any(keyword =>
            hospitalName.ToLower().Contains(keyword));

        return isValidPattern && hasMedicalKeyword;
    }

    private bool BeValidPrescriptionNumber(string prescriptionNumber)
    {
        if (string.IsNullOrWhiteSpace(prescriptionNumber))
            return true;

        // Allow alphanumeric with common separators
        var prescriptionPattern = @"^[a-zA-Z0-9\-\_\/\.\s]+$";
        return System.Text.RegularExpressions.Regex.IsMatch(prescriptionNumber, prescriptionPattern);
    }

    private bool BeValidQuantityUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            return true;

        var validUnits = new[]
        {
            "viên", "ml", "mg", "g", "kg", "lần", "giọt", "thìa", "túi", "gói",
            "cap", "capsule", "tablet", "mcg", "µg", "iu", "unit", "chai", "hộp"
        };

        return validUnits.Any(validUnit =>
            unit.ToLower().Trim() == validUnit);
    }

    private bool HaveValidDateRange(UpdateStudentMedicationRequest request)
    {
        if (!request.StartDate.HasValue || !request.EndDate.HasValue)
            return true;
        return request.EndDate.Value > request.StartDate.Value;
    }

    private bool HaveValidExpiryDate(UpdateStudentMedicationRequest request)
    {
        if (!request.ExpiryDate.HasValue)
            return true;

        if (request.EndDate.HasValue)
            return request.ExpiryDate.Value > request.EndDate.Value;

        if (request.StartDate.HasValue)
            return request.ExpiryDate.Value > request.StartDate.Value;

        return true;
    }

    private bool HaveValidPrescriptionDateRange(UpdateStudentMedicationRequest request)
    {
        if (!request.PrescriptionDate.HasValue || !request.StartDate.HasValue)
            return true;

        return request.PrescriptionDate.Value <= request.StartDate.Value;
    }

    private bool HaveAtLeastOneField(UpdateStudentMedicationRequest request)
    {
        return !string.IsNullOrEmpty(request.MedicationName) ||
               !string.IsNullOrEmpty(request.Dosage) ||
               !string.IsNullOrEmpty(request.Instructions) ||
               !string.IsNullOrEmpty(request.Frequency) ||
               request.StartDate.HasValue ||
               request.EndDate.HasValue ||
               request.ExpiryDate.HasValue ||
               !string.IsNullOrEmpty(request.Purpose) ||
               !string.IsNullOrEmpty(request.DoctorName) ||
               !string.IsNullOrEmpty(request.Hospital) ||
               request.PrescriptionDate.HasValue ||
               !string.IsNullOrEmpty(request.PrescriptionNumber) ||
               request.QuantitySent.HasValue ||
               !string.IsNullOrEmpty(request.QuantityUnit) ||
               !string.IsNullOrEmpty(request.SideEffects) ||
               !string.IsNullOrEmpty(request.StorageInstructions) ||
               !string.IsNullOrEmpty(request.SpecialNotes) ||
               !string.IsNullOrEmpty(request.EmergencyContactInstructions) ||
               request.Priority.HasValue ||
               request.TimeOfDay.HasValue;
    }

    private bool HaveReasonableUpdateWindow(UpdateStudentMedicationRequest request)
    {
        if (!request.StartDate.HasValue || !request.EndDate.HasValue)
            return true;

        var duration = request.EndDate.Value - request.StartDate.Value;
        return duration.TotalDays >= 1 && duration.TotalDays <= 365;
    }

    #endregion
}
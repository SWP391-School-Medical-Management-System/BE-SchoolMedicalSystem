namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

public enum HealthEventType
{
    Injury,                // Chấn thương
    Illness,               // Ốm đau
    AllergicReaction,      // Phản ứng dị ứng
    Fall,                  // Té ngã
    ChronicIllnessEpisode, // Đợt tái phát bệnh mãn tính
    Other                  // Khác
}

// Injury -> MedicalHistory (nếu là tiền sử chấn thương)
// Illness -> MedicalHistory (nếu là tiền sử bệnh)
// AllergicReaction -> Allergy
// Fall -> MedicalHistory (nếu là tiền sử té ngã)
// ChronicIllnessEpisode -> ChronicDisease
// Other -> MedicalHistory hoặc không liên quan
namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public interface IMedicationCleanupJob
{
    Task CleanupExpiredDataAsync();
    Task CleanupExpiredMedicationsAsync();
    Task CleanupOldNotificationsAsync();
    Task CleanupOldAdministrationRecordsAsync();
}
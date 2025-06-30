namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public interface IMedicationScheduleJob
{
    Task ProcessTodayMedicationsAsync();
    Task ProcessTomorrowMedicationsAsync();
    Task ProcessNewlyApprovedMedicationsAsync();
    Task ProcessApprovedToActiveTransitionAsync();
}
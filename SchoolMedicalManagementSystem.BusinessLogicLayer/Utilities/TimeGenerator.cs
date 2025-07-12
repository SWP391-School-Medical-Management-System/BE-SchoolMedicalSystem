using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public static class TimeGenerator
{
    public static List<MedicationTimeOfDay> GenerateTimesFromFrequency(int frequencyCount)
    {
        return frequencyCount switch
        {
            1 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.Morning },
            2 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.Noon},
            3 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.Morning, MedicationTimeOfDay.Noon, MedicationTimeOfDay.Afternoon },
            _ => new List<MedicationTimeOfDay> { MedicationTimeOfDay.Evening }
        };
    }
    
    public static List<TimeSpan> GenerateTimeSpansFromFrequency(int frequencyCount)
    {
        return frequencyCount switch
        {
            1 => new List<TimeSpan> { new TimeSpan(8, 30, 0) }, 
            2 => new List<TimeSpan> { new TimeSpan(8, 30, 0), new TimeSpan(11, 0, 0) },  
            3 => new List<TimeSpan> { new TimeSpan(7, 0, 0), new TimeSpan(11, 0, 0), new TimeSpan(16, 0, 0) },  // Morning, AfterLunch, LateAfternoon
            _ => new List<TimeSpan> { new TimeSpan(18, 0, 0) }
        };
    }
} 
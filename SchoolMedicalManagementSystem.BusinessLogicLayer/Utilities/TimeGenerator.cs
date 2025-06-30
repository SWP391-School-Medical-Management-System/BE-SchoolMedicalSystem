using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public static class TimeGenerator
{
    public static List<MedicationTimeOfDay> GenerateTimesFromFrequency(int frequencyCount)
    {
        return frequencyCount switch
        {
            1 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.AfterBreakfast },
            2 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.AfterBreakfast, MedicationTimeOfDay.AfterLunch },
            3 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.Morning, MedicationTimeOfDay.AfterLunch, MedicationTimeOfDay.LateAfternoon },
            4 => new List<MedicationTimeOfDay> { MedicationTimeOfDay.Morning, MedicationTimeOfDay.MidMorning, MedicationTimeOfDay.AfterLunch, MedicationTimeOfDay.MidAfternoon },
            _ => new List<MedicationTimeOfDay> { MedicationTimeOfDay.AfterBreakfast }
        };
    }
    
    public static List<TimeSpan> GenerateTimeSpansFromFrequency(int frequencyCount)
    {
        return frequencyCount switch
        {
            1 => new List<TimeSpan> { new TimeSpan(8, 30, 0) },  // AfterBreakfast
            2 => new List<TimeSpan> { new TimeSpan(8, 30, 0), new TimeSpan(13, 0, 0) },  // AfterBreakfast, AfterLunch
            3 => new List<TimeSpan> { new TimeSpan(7, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(16, 0, 0) },  // Morning, AfterLunch, LateAfternoon
            4 => new List<TimeSpan> { new TimeSpan(7, 0, 0), new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(14, 30, 0) },  // Morning, MidMorning, AfterLunch, MidAfternoon
            _ => new List<TimeSpan> { new TimeSpan(8, 30, 0) }
        };
    }
} 
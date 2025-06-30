using System.Text.RegularExpressions;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public static class FrequencyParser
{
    public static (int count, string unit) ParseFrequency(string frequency)
    {
        if (string.IsNullOrEmpty(frequency))
            return (1, "lần/ngày");
            
        var patterns = new[]
        {
            @"(\d+)\s*lần\s*/\s*ngày",
            @"(\d+)\s*lần\s*/\s*tuần", 
            @"(\d+)\s*lần\s*/\s*tháng"
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(frequency, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var count = int.Parse(match.Groups[1].Value);
                var unit = match.Value.Contains("/ngày") ? "lần/ngày" :
                          match.Value.Contains("/tuần") ? "lần/tuần" : "lần/tháng";
                return (count, unit);
            }
        }
        
        return (1, "lần/ngày"); // Default
    }
} 
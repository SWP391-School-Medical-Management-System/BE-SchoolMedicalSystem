using Hangfire;
using Microsoft.Extensions.Configuration;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer;

public static class HangfireDashboardOptionsExtensions
{
    public static DashboardOptions GetDashboardOptions(IConfiguration configuration)
    {
        var hangfireConfig = configuration.GetSection("Hangfire");
        var isDashboardEnabled = hangfireConfig.GetValue<bool>("DashboardEnabled", true);

        if (!isDashboardEnabled)
        {
            return new DashboardOptions
            {
                Authorization = new[] { new DenyAllAuthorizationFilter() }
            };
        }

        return new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DashboardTitle = "School Medical System - Background Jobs",
            StatsPollingInterval = 2000,
            DisplayStorageConnectionString = false,
            IsReadOnlyFunc = (context) => false,
            IgnoreAntiforgeryToken = true
        };
    }
}
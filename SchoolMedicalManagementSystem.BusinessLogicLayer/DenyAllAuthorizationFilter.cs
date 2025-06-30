using Hangfire.Dashboard;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer;

public class DenyAllAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => false;
}
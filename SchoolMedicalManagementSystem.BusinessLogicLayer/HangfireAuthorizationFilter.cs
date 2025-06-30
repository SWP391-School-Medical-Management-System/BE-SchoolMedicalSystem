using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userRoles = httpContext.User.Claims
                .Where(c => c.Type == "r")
                .Select(c => c.Value)
                .ToList();

            return userRoles.Any(role =>
                role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("MANAGER", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("SCHOOLNURSE", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
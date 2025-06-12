using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;

public static class UserHelper
{
    public static string GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }

        return userIdClaim;
    }

    public static string GetCurrentUserEmail(HttpContext httpContext)
    {
        var emailClaim = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(emailClaim))
        {
            throw new UnauthorizedAccessException("Email not found in token.");
        }

        return emailClaim;
    }

    public static async Task<List<string>> GetCurrentUserRolesFromDatabaseAsync(
        HttpContext httpContext,
        IUnitOfWork unitOfWork)
    {
        try
        {
            var userId = GetCurrentUserId(httpContext);
            var userGuid = Guid.Parse(userId);

            var userRepo = unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userGuid && !u.IsDeleted && u.IsActive);

            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found or inactive.");
            }

            return user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role.Name)
                .ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    public static async Task<bool> HasRoleAsync(
        HttpContext httpContext,
        IUnitOfWork unitOfWork,
        string roleName)
    {
        var roles = await GetCurrentUserRolesFromDatabaseAsync(httpContext, unitOfWork);
        return roles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<bool> HasAnyRoleAsync(
        HttpContext httpContext,
        IUnitOfWork unitOfWork,
        params string[] roleNames)
    {
        var userRoles = await GetCurrentUserRolesFromDatabaseAsync(httpContext, unitOfWork);
        return roleNames.Any(roleName =>
            userRoles.Any(userRole => userRole.Equals(roleName, StringComparison.OrdinalIgnoreCase)));
    }

    public static List<string> GetCurrentUserRoles(HttpContext httpContext)
    {
        try
        {
            var claims = httpContext.User?.Claims;
            if (claims == null) return new List<string>();

            var roles = new List<string>();

            var possibleRoleClaimTypes = new[]
            {
                ClaimTypes.Role,
                "role",
                "roles",
                "user_role",
                "userRole"
            };

            foreach (var claimType in possibleRoleClaimTypes)
            {
                var roleClaims = claims.Where(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value)
                    .ToList();

                if (roleClaims.Any())
                {
                    roles.AddRange(roleClaims);
                    break;
                }
            }

            if (roles.Count == 1 && roles[0].Contains(','))
            {
                roles = roles[0].Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToList();
            }

            return roles.Distinct().ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }
}
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Services;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Services.AuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Services.EmailService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogicLayer(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddMemoryCache();

        // Register services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICacheService, CacheService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateManagerRequestValidator>();

        // Mappers
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // HttpContextAccessor
        services.AddHttpContextAccessor();

        // Add Http Context Accessor
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();

        return services;
    }
}
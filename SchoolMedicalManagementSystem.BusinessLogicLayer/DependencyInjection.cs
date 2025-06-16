using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Services;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Services.AuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Services.EmailService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalCondition;
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
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ISchoolClassService, SchoolClassService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IMedicalConditionService, MedicalConditionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMedicalItemService, MedicalItemService>();
        services.AddScoped<IHealthEventService, HealthEventService>();
        services.AddScoped<IStudentMedicationService, StudentMedicationService>();
        services.AddScoped<IVaccinationRecordService, VaccinationRecordService>();
        services.AddScoped<IVaccinationService, VaccinationService>();
        // Cloudinary
        services.AddScoped<CloudinaryService>();
        // Excel
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateManagerRequestValidator>();

        // Mappers
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // HttpContextAccessor
        services.AddHttpContextAccessor();

        // Background Service
        services.AddHostedService<HealthEventBackgroundService>();

        // Add Http Context Accessor
        services.AddDistributedMemoryCache();

        return services;
    }
}
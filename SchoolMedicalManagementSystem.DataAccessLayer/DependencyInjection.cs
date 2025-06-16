using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolMedicalManagementSystem.DataAccessLayer.Repositories;
using SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.DataAccessLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<ISchoolClassRepository, SchoolClassRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
        services.AddScoped<IMedicalConditionRepository, MedicalConditionRepository>();
        services.AddScoped<IMedicalItemRepository, MedicalItemRepository>();
        services.AddScoped<IHealthEventRepository, HealthEventRepository>();
        services.AddScoped<IStudentMedicationRepository, StudentMedicationRepository>();
        services.AddScoped<IVaccinationRecordRepository, VaccinationRecordRepository>();
        services.AddScoped<IVaccinationTypeRepository, VaccinationTypeRepository>();

        return services;
    }
}
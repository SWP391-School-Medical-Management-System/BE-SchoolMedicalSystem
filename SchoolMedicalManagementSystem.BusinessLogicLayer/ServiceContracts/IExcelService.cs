using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IExcelService
{
    // School Class
    Task<byte[]> GenerateSchoolClassTemplateAsync();
    Task<ExcelImportResult<SchoolClassExcelModel>> ReadSchoolClassExcelAsync(IFormFile file);
    Task<byte[]> ExportSchoolClassesToExcelAsync(List<SchoolClassResponse> classes);

    // Manager
    Task<byte[]> GenerateManagerTemplateAsync();
    Task<ExcelImportResult<ManagerExcelModel>> ReadManagerExcelAsync(IFormFile file);
    Task<byte[]> ExportManagersToExcelAsync(List<StaffUserResponse> managers);

    // School Nurse
    Task<byte[]> GenerateSchoolNurseTemplateAsync();
    Task<ExcelImportResult<SchoolNurseExcelModel>> ReadSchoolNurseExcelAsync(IFormFile file);
    Task<byte[]> ExportSchoolNursesToExcelAsync(List<StaffUserResponse> nurses);

    // Student and Parent
    Task<byte[]> GenerateStudentParentCombinedTemplateAsync();
    Task<ExcelImportResult<StudentParentCombinedExcelModel>> ReadStudentParentCombinedExcelAsync(IFormFile file);
}
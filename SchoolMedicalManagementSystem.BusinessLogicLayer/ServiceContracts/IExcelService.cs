using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IExcelService
{
    Task<byte[]> GenerateSchoolClassTemplateAsync();
    Task<ExcelImportResult<SchoolClassExcelModel>> ReadSchoolClassExcelAsync(IFormFile file);
    Task<byte[]> ExportSchoolClassesToExcelAsync(List<SchoolClassResponse> classes);
    Task<byte[]> GenerateManagerTemplateAsync();
    Task<ExcelImportResult<ManagerExcelModel>> ReadManagerExcelAsync(IFormFile file);
    Task<byte[]> ExportManagersToExcelAsync(List<StaffUserResponse> managers);
    Task<byte[]> GenerateSchoolNurseTemplateAsync();
    Task<ExcelImportResult<SchoolNurseExcelModel>> ReadSchoolNurseExcelAsync(IFormFile file);
    Task<byte[]> ExportSchoolNursesToExcelAsync(List<StaffUserResponse> nurses);
    Task<byte[]> GenerateStudentTemplateAsync();
    Task<ExcelImportResult<StudentExcelModel>> ReadStudentExcelAsync(IFormFile file);
    Task<byte[]> ExportStudentsToExcelAsync(List<StudentResponse> students);
    Task<byte[]> GenerateParentTemplateAsync();
    Task<ExcelImportResult<ParentExcelModel>> ReadParentExcelAsync(IFormFile file);
    Task<byte[]> ExportParentsToExcelAsync(List<ParentResponse> parents);
}
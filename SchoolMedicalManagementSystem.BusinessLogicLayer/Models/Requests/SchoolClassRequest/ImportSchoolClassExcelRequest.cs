using Microsoft.AspNetCore.Http;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

public class ImportSchoolClassExcelRequest
{
    public IFormFile ExcelFile { get; set; }
    public bool OverwriteExisting { get; set; } = false;
    //OverwriteExisting = false: Bỏ qua, không import lớp trùng
    //OverwriteExisting = true: Cập nhật lớp hiện có với dữ liệu mới
}
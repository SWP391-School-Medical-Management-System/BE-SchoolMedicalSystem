using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System.Drawing;
using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;
using System.Globalization;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class ExcelService : IExcelService
{
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(ILogger<ExcelService> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    #region School Class Excel Operations

    public async Task<byte[]> GenerateSchoolClassTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();

            var worksheet = package.Workbook.Worksheets.Add("Template_LopHoc");

            var instructionSheet = package.Workbook.Worksheets.Add("Huong_Dan");

            await CreateSchoolClassTemplateSheet(worksheet);

            await CreateInstructionSheet(instructionSheet);

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating school class template");
            throw;
        }
    }

    public async Task<ExcelImportResult<SchoolClassExcelModel>> ReadSchoolClassExcelAsync(IFormFile file)
    {
        var result = new ExcelImportResult<SchoolClassExcelModel>();

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Success = false;
                result.Message = "Không tìm thấy worksheet trong file Excel.";
                return result;
            }

            var classes = new List<SchoolClassExcelModel>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var currentYear = DateTime.Now.Year;

            for (int row = 3; row <= rowCount; row++)
            {
                var name = worksheet.Cells[row, 1].Text?.Trim();
                var gradeText = worksheet.Cells[row, 2].Text?.Trim();
                var academicYearText = worksheet.Cells[row, 3].Text?.Trim();

                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(gradeText) &&
                    string.IsNullOrEmpty(academicYearText))
                    continue;

                var classModel = new SchoolClassExcelModel
                {
                    Name = name
                };

                var errors = new List<string>();

                if (int.TryParse(gradeText, out int grade))
                {
                    classModel.Grade = grade;
                    if (grade < 1 || grade > 12)
                    {
                        errors.Add("Khối lớp phải từ 1 đến 12");
                    }
                }
                else
                {
                    errors.Add("Khối lớp phải là số nguyên từ 1 đến 12");
                }

                if (int.TryParse(academicYearText, out int academicYear))
                {
                    classModel.AcademicYear = academicYear;
                    if (academicYear < currentYear - 1 || academicYear > currentYear + 2)
                    {
                        errors.Add($"Năm học phải trong khoảng {currentYear - 1} đến {currentYear + 2}");
                    }
                }
                else
                {
                    errors.Add("Năm học phải là số nguyên");
                }

                if (string.IsNullOrEmpty(name))
                {
                    errors.Add("Tên lớp học không được để trống");
                }
                else if (name.Length > 20)
                {
                    errors.Add("Tên lớp học không được quá 20 ký tự");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9\s]+$"))
                {
                    errors.Add("Tên lớp học chỉ được chứa chữ cái, số và khoảng trắng");
                }

                classModel.IsValid = !errors.Any();
                classModel.ErrorMessage = string.Join("; ", errors);

                classes.Add(classModel);
            }

            result.TotalRows = classes.Count;
            result.ValidData = classes.Where(c => c.IsValid).ToList();
            result.InvalidData = classes.Where(c => !c.IsValid).ToList();
            result.SuccessRows = result.ValidData.Count;
            result.ErrorRows = result.InvalidData.Count;
            result.Success = result.TotalRows > 0;
            result.Message = result.Success ? "Đọc file Excel thành công." : "File Excel không có dữ liệu hợp lệ.";

            if (result.InvalidData.Any())
            {
                result.Errors = result.InvalidData.Select(c => $"Lớp {c.Name}: {c.ErrorMessage}").ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading school class Excel file");
            result.Success = false;
            result.Message = $"Lỗi đọc file Excel: {ex.Message}";
            return result;
        }
    }

    public async Task<byte[]> ExportSchoolClassesToExcelAsync(List<SchoolClassResponse> classes)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh_Sach_Lop_Hoc");

            var headers = new[]
            {
                "STT", "Tên Lớp", "Khối", "Năm Học", "Số Học Sinh",
                "Male", "Female", "Ngày Tạo", "Cập Nhật Lần Cuối"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }

            for (int i = 0; i < classes.Count; i++)
            {
                var classItem = classes[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = classItem.Name;
                worksheet.Cells[row, 3].Value = classItem.Grade;
                worksheet.Cells[row, 4].Value = classItem.AcademicYear;
                worksheet.Cells[row, 5].Value = classItem.StudentCount;
                worksheet.Cells[row, 6].Value = classItem.MaleStudentCount;
                worksheet.Cells[row, 7].Value = classItem.FemaleStudentCount;
                worksheet.Cells[row, 8].Value = classItem.CreatedDate?.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[row, 9].Value = classItem.LastUpdatedDate?.ToString("dd/MM/yyyy HH:mm");
            }

            worksheet.Cells.AutoFitColumns();

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting school classes to Excel");
            throw;
        }
    }

    #endregion

    #region Manager Excel Operations

    public async Task<byte[]> GenerateManagerTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template_Manager");
            var instructionSheet = package.Workbook.Worksheets.Add("Huong_Dan");

            await CreateManagerTemplateSheet(worksheet);
            await CreateManagerInstructionSheet(instructionSheet);

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating manager template");
            throw;
        }
    }

    public async Task<ExcelImportResult<ManagerExcelModel>> ReadManagerExcelAsync(IFormFile file)
    {
        var result = new ExcelImportResult<ManagerExcelModel>();

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Success = false;
                result.Message = "Không tìm thấy worksheet trong file Excel.";
                return result;
            }

            var managers = new List<ManagerExcelModel>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 3; row <= rowCount; row++)
            {
                var manager = ReadManagerFromRow(worksheet, row);
                if (manager != null)
                    managers.Add(manager);
            }

            result.TotalRows = managers.Count;
            result.ValidData = managers.Where(m => m.IsValid).ToList();
            result.InvalidData = managers.Where(m => !m.IsValid).ToList();
            result.SuccessRows = result.ValidData.Count;
            result.ErrorRows = result.InvalidData.Count;
            result.Success = result.TotalRows > 0;
            result.Message = result.Success ? "Đọc file Excel thành công." : "File Excel không có dữ liệu hợp lệ.";

            if (result.InvalidData.Any())
            {
                result.Errors = result.InvalidData.Select(m => $"Dòng {managers.IndexOf(m) + 3}: {m.ErrorMessage}")
                    .ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading manager Excel file");
            result.Success = false;
            result.Message = $"Lỗi đọc file Excel: {ex.Message}";
            return result;
        }
    }

    public async Task<byte[]> ExportManagersToExcelAsync(List<StaffUserResponse> managers)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh_Sach_Manager");

            var headers = new[]
            {
                "STT", "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
                "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mã Nhân Viên", "Ngày Tạo"
            };

            CreateHeaderRow(worksheet, headers);

            for (int i = 0; i < managers.Count; i++)
            {
                var manager = managers[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = manager.Username;
                worksheet.Cells[row, 3].Value = manager.Email;
                worksheet.Cells[row, 4].Value = manager.FullName;
                worksheet.Cells[row, 5].Value = manager.PhoneNumber;
                worksheet.Cells[row, 6].Value = manager.Address;
                worksheet.Cells[row, 7].Value = manager.Gender;
                worksheet.Cells[row, 8].Value = manager.DateOfBirth?.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 9].Value = manager.StaffCode;
                worksheet.Cells[row, 10].Value = manager.CreatedDate?.ToString("dd/MM/yyyy HH:mm");
            }

            worksheet.Cells.AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting managers to Excel");
            throw;
        }
    }

    #endregion

    #region School Nurse Excel Operations

    public async Task<byte[]> GenerateSchoolNurseTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template_SchoolNurse");
            var instructionSheet = package.Workbook.Worksheets.Add("Huong_Dan");

            await CreateSchoolNurseTemplateSheet(worksheet);
            await CreateSchoolNurseInstructionSheet(instructionSheet);

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating school nurse template");
            throw;
        }
    }

    public async Task<ExcelImportResult<SchoolNurseExcelModel>> ReadSchoolNurseExcelAsync(IFormFile file)
    {
        var result = new ExcelImportResult<SchoolNurseExcelModel>();

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Success = false;
                result.Message = "Không tìm thấy worksheet trong file Excel.";
                return result;
            }

            var nurses = new List<SchoolNurseExcelModel>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 3; row <= rowCount; row++)
            {
                var nurse = ReadSchoolNurseFromRow(worksheet, row);
                if (nurse != null)
                    nurses.Add(nurse);
            }

            result.TotalRows = nurses.Count;
            result.ValidData = nurses.Where(n => n.IsValid).ToList();
            result.InvalidData = nurses.Where(n => !n.IsValid).ToList();
            result.SuccessRows = result.ValidData.Count;
            result.ErrorRows = result.InvalidData.Count;
            result.Success = result.TotalRows > 0;
            result.Message = result.Success ? "Đọc file Excel thành công." : "File Excel không có dữ liệu hợp lệ.";

            if (result.InvalidData.Any())
            {
                result.Errors = result.InvalidData.Select(n => $"Dòng {nurses.IndexOf(n) + 3}: {n.ErrorMessage}")
                    .ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading school nurse Excel file");
            result.Success = false;
            result.Message = $"Lỗi đọc file Excel: {ex.Message}";
            return result;
        }
    }

    public async Task<byte[]> ExportSchoolNursesToExcelAsync(List<StaffUserResponse> nurses)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh_Sach_School_Nurse");

            var headers = new[]
            {
                "STT", "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
                "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mã Nhân Viên", "Số Chứng Chỉ", "Chuyên Môn", "Ngày Tạo"
            };

            CreateHeaderRow(worksheet, headers);

            for (int i = 0; i < nurses.Count; i++)
            {
                var nurse = nurses[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = nurse.Username;
                worksheet.Cells[row, 3].Value = nurse.Email;
                worksheet.Cells[row, 4].Value = nurse.FullName;
                worksheet.Cells[row, 5].Value = nurse.PhoneNumber;
                worksheet.Cells[row, 6].Value = nurse.Address;
                worksheet.Cells[row, 7].Value = nurse.Gender;
                worksheet.Cells[row, 8].Value = nurse.DateOfBirth?.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 9].Value = nurse.StaffCode;
                worksheet.Cells[row, 10].Value = nurse.LicenseNumber;
                worksheet.Cells[row, 11].Value = nurse.Specialization;
                worksheet.Cells[row, 12].Value = nurse.CreatedDate?.ToString("dd/MM/yyyy HH:mm");
            }

            worksheet.Cells.AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting school nurses to Excel");
            throw;
        }
    }

    #endregion

    #region Student-Parent Combined Excel Operations

    public async Task<byte[]> GenerateStudentParentCombinedTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template_HocSinh_PhuHuynh");
            var instructionSheet = package.Workbook.Worksheets.Add("Huong_Dan");

            await CreateStudentParentCombinedTemplateSheet(worksheet);
            await CreateStudentParentCombinedInstructionSheet(instructionSheet);

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating student-parent combined template");
            throw;
        }
    }

    public async Task<ExcelImportResult<StudentParentCombinedExcelModel>> ReadStudentParentCombinedExcelAsync(
        IFormFile file)
    {
        var result = new ExcelImportResult<StudentParentCombinedExcelModel>();

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Success = false;
                result.Message = "Không tìm thấy worksheet trong file Excel.";
                return result;
            }

            var combinedData = new List<StudentParentCombinedExcelModel>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            _logger.LogInformation("Reading Excel file with {RowCount} rows", rowCount);

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var data = ReadStudentParentFromRow(worksheet, row);
                    if (data != null)
                    {
                        _logger.LogInformation("Successfully read row {Row}: Student {Username}", row,
                            data.StudentUsername);
                        combinedData.Add(data);
                    }
                    else
                    {
                        _logger.LogInformation("Skipped empty row {Row}", row);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading row {Row}", row);
                    var errorData = new StudentParentCombinedExcelModel
                    {
                        StudentUsername = $"Row_{row}",
                        IsValid = false,
                        ErrorMessage = $"Lỗi đọc dòng {row}: {ex.Message}"
                    };
                    combinedData.Add(errorData);
                }
            }

            result.TotalRows = combinedData.Count;
            result.ValidData = combinedData.Where(d => d.IsValid).ToList();
            result.InvalidData = combinedData.Where(d => !d.IsValid).ToList();
            result.SuccessRows = result.ValidData.Count;
            result.ErrorRows = result.InvalidData.Count;

            result.Success = result.TotalRows > 0;
            result.Message = result.TotalRows > 0
                ? $"Đọc file Excel thành công. Tổng: {result.TotalRows}, Hợp lệ: {result.SuccessRows}, Lỗi: {result.ErrorRows}"
                : "File Excel không có dữ liệu.";

            if (result.InvalidData.Any())
            {
                result.Errors = result.InvalidData.Select(d =>
                    $"Dòng {combinedData.IndexOf(d) + 2}: {d.ErrorMessage}").ToList();
            }

            _logger.LogInformation("Excel reading completed. Total: {Total}, Valid: {Valid}, Invalid: {Invalid}",
                result.TotalRows, result.SuccessRows, result.ErrorRows);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading student-parent combined Excel file");
            result.Success = false;
            result.Message = $"Lỗi đọc file Excel: {ex.Message}";
            return result;
        }
    }

    #endregion

    #region Private Helper Methods

    private void CreateHeaderRow(ExcelWorksheet worksheet, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }
    }

    private ManagerExcelModel ReadManagerFromRow(ExcelWorksheet worksheet, int row)
    {
        var username = worksheet.Cells[row, 1].Text?.Trim();
        var email = worksheet.Cells[row, 2].Text?.Trim();
        var fullName = worksheet.Cells[row, 3].Text?.Trim();
        var phoneNumber = worksheet.Cells[row, 4].Text?.Trim();
        var address = worksheet.Cells[row, 5].Text?.Trim();
        var gender = worksheet.Cells[row, 6].Text?.Trim();
        var dateOfBirthText = "";
        var dateCell = worksheet.Cells[row, 7];
        var staffCode = worksheet.Cells[row, 8].Text?.Trim();

        if (dateCell.Value != null)
        {
            if (dateCell.Value is DateTime dateValue)
            {
                dateOfBirthText = dateValue.ToString("dd/MM/yyyy");
            }
            else if (dateCell.Value is double serialDate)
            {
                try
                {
                    var date = DateTime.FromOADate(serialDate);
                    dateOfBirthText = date.ToString("dd/MM/yyyy");
                }
                catch
                {
                    dateOfBirthText = dateCell.Text?.Trim();
                }
            }
            else
            {
                dateOfBirthText = dateCell.Text?.Trim();
            }
        }

        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(fullName))
            return null;

        var manager = new ManagerExcelModel
        {
            Username = username,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Address = address,
            Gender = gender,
            StaffCode = staffCode
        };

        ValidateManagerData(manager, dateOfBirthText);
        return manager;
    }

    private void ValidateManagerData(ManagerExcelModel manager, string dateOfBirthText)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(manager.Username))
            errors.Add("Tên đăng nhập không được để trống");

        if (string.IsNullOrEmpty(manager.Email))
            errors.Add("Email không được để trống");
        else if (!IsValidEmail(manager.Email))
            errors.Add("Email không đúng định dạng");

        if (string.IsNullOrEmpty(manager.FullName))
            errors.Add("Họ tên không được để trống");

        if (string.IsNullOrEmpty(manager.StaffCode))
            errors.Add("Mã nhân viên không được để trống");

        if (!string.IsNullOrEmpty(dateOfBirthText))
        {
            if (DateTime.TryParse(dateOfBirthText, out DateTime dateOfBirth))
            {
                manager.DateOfBirth = dateOfBirth;
                if (dateOfBirth > DateTime.Now.AddYears(-18))
                    errors.Add("Tuổi phải từ 18 trở lên");
            }
            else
            {
                errors.Add("Ngày sinh không đúng định dạng");
            }
        }

        if (!string.IsNullOrEmpty(manager.Gender) &&
            !new[] { "Male", "Female", "Other" }.Contains(manager.Gender))
            errors.Add("Giới tính phải là Male/Female hoặc Other");

        manager.IsValid = !errors.Any();
        manager.ErrorMessage = string.Join("; ", errors);
    }

    private SchoolNurseExcelModel ReadSchoolNurseFromRow(ExcelWorksheet worksheet, int row)
    {
        var username = worksheet.Cells[row, 1].Text?.Trim();
        var email = worksheet.Cells[row, 2].Text?.Trim();
        var fullName = worksheet.Cells[row, 3].Text?.Trim();
        var phoneNumber = worksheet.Cells[row, 4].Text?.Trim();
        var address = worksheet.Cells[row, 5].Text?.Trim();
        var gender = worksheet.Cells[row, 6].Text?.Trim();
        var dateOfBirthText = "";
        var dateCell = worksheet.Cells[row, 7];
        var staffCode = worksheet.Cells[row, 8].Text?.Trim();
        var licenseNumber = worksheet.Cells[row, 9].Text?.Trim();
        var specialization = worksheet.Cells[row, 10].Text?.Trim();

        if (dateCell.Value != null)
        {
            if (dateCell.Value is DateTime dateValue)
            {
                dateOfBirthText = dateValue.ToString("dd/MM/yyyy");
            }
            else if (dateCell.Value is double serialDate)
            {
                try
                {
                    var date = DateTime.FromOADate(serialDate);
                    dateOfBirthText = date.ToString("dd/MM/yyyy");
                }
                catch
                {
                    dateOfBirthText = dateCell.Text?.Trim();
                }
            }
            else
            {
                dateOfBirthText = dateCell.Text?.Trim();
            }
        }

        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(fullName))
            return null;

        var nurse = new SchoolNurseExcelModel
        {
            Username = username,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Address = address,
            Gender = gender,
            StaffCode = staffCode,
            LicenseNumber = licenseNumber,
            Specialization = specialization
        };

        ValidateSchoolNurseData(nurse, dateOfBirthText);
        return nurse;
    }

    private void ValidateSchoolNurseData(SchoolNurseExcelModel nurse, string dateOfBirthText)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(nurse.Username))
            errors.Add("Tên đăng nhập không được để trống");

        if (string.IsNullOrEmpty(nurse.Email))
            errors.Add("Email không được để trống");
        else if (!IsValidEmail(nurse.Email))
            errors.Add("Email không đúng định dạng");

        if (string.IsNullOrEmpty(nurse.FullName))
            errors.Add("Họ tên không được để trống");

        if (string.IsNullOrEmpty(nurse.StaffCode))
            errors.Add("Mã nhân viên không được để trống");

        if (string.IsNullOrEmpty(nurse.LicenseNumber))
            errors.Add("Số chứng chỉ không được để trống");

        if (!string.IsNullOrEmpty(dateOfBirthText))
        {
            if (DateTime.TryParse(dateOfBirthText, out DateTime dateOfBirth))
            {
                nurse.DateOfBirth = dateOfBirth;
                if (dateOfBirth > DateTime.Now.AddYears(-18))
                    errors.Add("Tuổi phải từ 18 trở lên");
            }
            else
            {
                errors.Add("Ngày sinh không đúng định dạng");
            }
        }

        if (!string.IsNullOrEmpty(nurse.Gender) &&
            !new[] { "Male", "Female", "Other" }.Contains(nurse.Gender))
            errors.Add("Giới tính phải là Male/Female hoặc Other");

        nurse.IsValid = !errors.Any();
        nurse.ErrorMessage = string.Join("; ", errors);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task CreateManagerTemplateSheet(ExcelWorksheet worksheet)
    {
        var headers = new[]
        {
            "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
            "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mã Nhân Viên"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        worksheet.Cells[2, 1].Value = "manager001";
        worksheet.Cells[2, 2].Value = "manager@school.edu.vn";
        worksheet.Cells[2, 3].Value = "Nguyễn Văn A";
        worksheet.Cells[2, 4].Value = "0901234567";
        worksheet.Cells[2, 5].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[2, 6].Value = "Male";
        worksheet.Cells[2, 7].Value = "01/01/1980";
        worksheet.Cells[2, 8].Value = "MG001";

        using (var range = worksheet.Cells[2, 1, 2, headers.Length])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            range.Style.Font.Italic = true;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.Cells[2, 1].AddComment("Dòng này là ví dụ, bạn có thể xóa và nhập dữ liệu từ dòng 3", "System");
        await Task.CompletedTask;
    }

    private async Task CreateSchoolNurseTemplateSheet(ExcelWorksheet worksheet)
    {
        var headers = new[]
        {
            "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
            "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mã Nhân Viên", "Số Chứng Chỉ", "Chuyên Môn"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        worksheet.Cells[2, 1].Value = "nurse001";
        worksheet.Cells[2, 2].Value = "nurse@school.edu.vn";
        worksheet.Cells[2, 3].Value = "Trần Thị B";
        worksheet.Cells[2, 4].Value = "0912345678";
        worksheet.Cells[2, 5].Value = "456 Đường DEF, Quận 2, TP.HCM";
        worksheet.Cells[2, 6].Value = "Female";
        worksheet.Cells[2, 7].Value = "15/05/1985";
        worksheet.Cells[2, 8].Value = "NS001";
        worksheet.Cells[2, 9].Value = "YT123456";
        worksheet.Cells[2, 10].Value = "Y tế học đường";

        using (var range = worksheet.Cells[2, 1, 2, headers.Length])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            range.Style.Font.Italic = true;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.Cells[2, 1].AddComment("Dòng này là ví dụ, bạn có thể xóa và nhập dữ liệu từ dòng 3", "System");
        await Task.CompletedTask;
    }

    private async Task CreateManagerInstructionSheet(ExcelWorksheet worksheet)
    {
        var instructions = new[]
        {
            "HƯỚNG DẪN IMPORT MANAGER",
            "",
            "1. QUY TẮC CHUNG:",
            "   - File phải có định dạng Excel (.xlsx hoặc .xls)",
            "   - Kích thước file không vượt quá 10MB",
            "   - Không được để trống các cột bắt buộc",
            "",
            "2. CẤU TRÚC FILE:",
            "   - Cột A: Tên Đăng Nhập (bắt buộc, duy nhất)",
            "   - Cột B: Email (bắt buộc, duy nhất, đúng định dạng)",
            "   - Cột C: Họ Tên (bắt buộc)",
            "   - Cột D: Số Điện Thoại (tùy chọn)",
            "   - Cột E: Địa Chỉ (tùy chọn)",
            "   - Cột F: Giới Tính (Male/Female hoặc Other)",
            "   - Cột G: Ngày Sinh (định dạng dd/MM/yyyy, tuổi >= 18)",
            "   - Cột H: Mã Nhân Viên (bắt buộc, duy nhất)",
            "",
            "3. LƯU Ý:",
            "   - Hệ thống sẽ tự động tạo mật khẩu và gửi qua email",
            "   - Tên đăng nhập và email không được trùng với tài khoản hiện có",
            "   - Mã nhân viên phải duy nhất trong hệ thống"
        };

        await CreateInstructionContent(worksheet, instructions);
    }

    private async Task CreateSchoolNurseInstructionSheet(ExcelWorksheet worksheet)
    {
        var instructions = new[]
        {
            "HƯỚNG DẪN IMPORT SCHOOL NURSE",
            "",
            "1. QUY TẮC CHUNG:",
            "   - File phải có định dạng Excel (.xlsx hoặc .xls)",
            "   - Kích thước file không vượt quá 10MB",
            "   - Không được để trống các cột bắt buộc",
            "",
            "2. CẤU TRÚC FILE:",
            "   - Cột A: Tên Đăng Nhập (bắt buộc, duy nhất)",
            "   - Cột B: Email (bắt buộc, duy nhất, đúng định dạng)",
            "   - Cột C: Họ Tên (bắt buộc)",
            "   - Cột D: Số Điện Thoại (tùy chọn)",
            "   - Cột E: Địa Chỉ (tùy chọn)",
            "   - Cột F: Giới Tính (Male/Female hoặc Other)",
            "   - Cột G: Ngày Sinh (định dạng dd/MM/yyyy, tuổi >= 18)",
            "   - Cột H: Mã Nhân Viên (bắt buộc, duy nhất)",
            "   - Cột I: Số Chứng Chỉ (bắt buộc)",
            "   - Cột J: Chuyên Môn (tùy chọn)",
            "",
            "3. LƯU Ý:",
            "   - Hệ thống sẽ tự động tạo mật khẩu và gửi qua email",
            "   - Số chứng chỉ phải được cấp bởi cơ quan có thẩm quyền"
        };

        await CreateInstructionContent(worksheet, instructions);
    }

    private async Task CreateInstructionContent(ExcelWorksheet worksheet, string[] instructions)
    {
        for (int i = 0; i < instructions.Length; i++)
        {
            worksheet.Cells[i + 1, 1].Value = instructions[i];

            if (i == 0)
            {
                worksheet.Cells[i + 1, 1].Style.Font.Bold = true;
                worksheet.Cells[i + 1, 1].Style.Font.Size = 16;
                worksheet.Cells[i + 1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[i + 1, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }
            else if (instructions[i].StartsWith("1.") || instructions[i].StartsWith("2.") ||
                     instructions[i].StartsWith("3."))
            {
                worksheet.Cells[i + 1, 1].Style.Font.Bold = true;
                worksheet.Cells[i + 1, 1].Style.Font.Color.SetColor(Color.DarkBlue);
            }
        }

        worksheet.Column(1).Width = 80;
        worksheet.Column(1).Style.WrapText = true;
        await Task.CompletedTask;
    }

    private async Task CreateSchoolClassTemplateSheet(ExcelWorksheet worksheet)
    {
        worksheet.Cells[1, 1].Value = "Tên Lớp";
        worksheet.Cells[1, 2].Value = "Khối";
        worksheet.Cells[1, 3].Value = "Năm Học";

        using (var range = worksheet.Cells[1, 1, 1, 3])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        var currentYear = DateTime.Now.Year;
        worksheet.Cells[2, 1].Value = "1A";
        worksheet.Cells[2, 2].Value = 1;
        worksheet.Cells[2, 3].Value = currentYear;

        using (var range = worksheet.Cells[2, 1, 2, 3])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            range.Style.Font.Italic = true;
        }

        using (var range = worksheet.Cells[1, 1, 2, 3])
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        worksheet.Cells.AutoFitColumns();

        worksheet.Cells[2, 1].AddComment("Dòng này là ví dụ, bạn có thể xóa và nhập dữ liệu của mình từ dòng 3 trở đi",
            "System");

        await Task.CompletedTask;
    }

    private async Task CreateInstructionSheet(ExcelWorksheet worksheet)
    {
        var currentYear = DateTime.Now.Year;
        var instructions = new[]
        {
            "HƯỚNG DẪN IMPORT LỚP HỌC",
            "",
            "1. QUY TẮC CHUNG:",
            "   - File phải có định dạng Excel (.xlsx hoặc .xls)",
            "   - Kích thước file không vượt quá 10MB",
            "   - Không được để trống các cột bắt buộc",
            "",
            "2. CẤU TRÚC FILE:",
            "   - Cột A: Tên Lớp (bắt buộc, tối đa 20 ký tự, chỉ chứa chữ cái, số và khoảng trắng)",
            "   - Cột B: Khối (bắt buộc, số từ 1 đến 12)",
            $"   - Cột C: Năm Học (bắt buộc, từ {currentYear - 1} đến {currentYear + 2})",
            "",
            "3. LƯU Ý:",
            "   - Dòng 1: Tiêu đề cột (không được thay đổi)",
            "   - Dòng 2: Dữ liệu mẫu (có thể xóa)",
            "   - Từ dòng 3 trở đi: Nhập dữ liệu thực tế",
            "   - Tên lớp không được trùng trong cùng năm học",
            $"   - Năm học hiện tại: {currentYear}",
            $"   - Có thể tạo lớp cho năm {currentYear - 1} (import dữ liệu cũ)",
            $"   - Có thể tạo lớp cho năm {currentYear + 1}, {currentYear + 2} (chuẩn bị trước)",
            "",
            "4. VÍ DỤ DỮ LIỆU HỢP LỆ:",
            "   Tên Lớp    | Khối | Năm Học",
            $"   1A         | 1    | {currentYear}",
            $"   2B         | 2    | {currentYear + 1}",
            $"   3C         | 3    | {currentYear}",
            "",
            "5. CÁC LỖI THƯỜNG GẶP:",
            "   - Tên lớp để trống hoặc quá dài",
            "   - Khối không phải số hoặc ngoài khoảng 1-12",
            $"   - Năm học ngoài khoảng cho phép ({currentYear - 1} - {currentYear + 2})",
            "   - Tên lớp trùng lặp trong cùng năm học",
            "",
            "6. CÁCH SỬ DỤNG:",
            "   - Tải template này về máy",
            "   - Mở bằng Excel và chuyển sang sheet 'Template_LopHoc'",
            "   - Xóa dòng mẫu (dòng 2) và nhập dữ liệu của bạn",
            "   - Lưu file và upload lên hệ thống",
            "",
            "7. HỖ TRỢ:",
            "   - Nếu gặp lỗi, hệ thống sẽ hiển thị chi tiết lỗi của từng dòng",
            "   - Sửa lỗi trong file Excel và thử import lại",
            "   - Liên hệ quản trị viên nếu cần hỗ trợ thêm"
        };

        for (int i = 0; i < instructions.Length; i++)
        {
            worksheet.Cells[i + 1, 1].Value = instructions[i];

            if (i == 0)
            {
                worksheet.Cells[i + 1, 1].Style.Font.Bold = true;
                worksheet.Cells[i + 1, 1].Style.Font.Size = 16;
                worksheet.Cells[i + 1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[i + 1, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }
            else if (instructions[i].StartsWith("1.") || instructions[i].StartsWith("2.") ||
                     instructions[i].StartsWith("3.") || instructions[i].StartsWith("4.") ||
                     instructions[i].StartsWith("5.") || instructions[i].StartsWith("6.") ||
                     instructions[i].StartsWith("7."))
            {
                worksheet.Cells[i + 1, 1].Style.Font.Bold = true;
                worksheet.Cells[i + 1, 1].Style.Font.Color.SetColor(Color.DarkBlue);
            }
        }

        worksheet.Column(1).Width = 80;
        worksheet.Column(1).Style.WrapText = true;

        await Task.CompletedTask;
    }

    private StudentParentCombinedExcelModel ReadStudentParentFromRow(ExcelWorksheet worksheet, int row)
    {
        var studentUsername = worksheet.Cells[row, 1].Text?.Trim();
        var studentEmail = worksheet.Cells[row, 2].Text?.Trim();
        var studentFullName = worksheet.Cells[row, 3].Text?.Trim();
        var studentPhoneNumber = worksheet.Cells[row, 4].Text?.Trim();
        var studentAddress = worksheet.Cells[row, 5].Text?.Trim();
        var studentGender = worksheet.Cells[row, 6].Text?.Trim();
        var studentDateOfBirthText = GetDateValue(worksheet.Cells[row, 7]);
        var studentCode = worksheet.Cells[row, 8].Text?.Trim();
        var classInfo = worksheet.Cells[row, 9].Text?.Trim();

        var parentFullName = worksheet.Cells[row, 10].Text?.Trim();
        var parentPhoneNumber = worksheet.Cells[row, 11].Text?.Trim();
        var parentEmail = worksheet.Cells[row, 12].Text?.Trim();
        var parentAddress = worksheet.Cells[row, 13].Text?.Trim();
        var parentGender = worksheet.Cells[row, 14].Text?.Trim();
        var parentDateOfBirthText = GetDateValue(worksheet.Cells[row, 15]);
        var parentRelationship = worksheet.Cells[row, 16].Text?.Trim();

        if (string.IsNullOrEmpty(studentUsername) &&
            string.IsNullOrEmpty(studentEmail) &&
            string.IsNullOrEmpty(studentFullName) &&
            string.IsNullOrEmpty(studentCode) &&
            string.IsNullOrEmpty(studentPhoneNumber))
        {
            return null;
        }

        var data = new StudentParentCombinedExcelModel
        {
            StudentUsername = studentUsername,
            StudentEmail = studentEmail,
            StudentFullName = studentFullName,
            StudentPhoneNumber = studentPhoneNumber,
            StudentAddress = studentAddress,
            StudentGender = studentGender,
            StudentCode = studentCode,
            ClassNames = classInfo,
            ParentFullName = parentFullName,
            ParentPhoneNumber = parentPhoneNumber,
            ParentEmail = parentEmail,
            ParentAddress = parentAddress,
            ParentGender = parentGender,
            ParentRelationship = parentRelationship
        };

        if (!string.IsNullOrEmpty(classInfo))
        {
            ParseClassInformation(data, classInfo);
        }

        if (!string.IsNullOrEmpty(studentDateOfBirthText))
        {
            if (TryParseDateSafely(studentDateOfBirthText, out DateTime studentDob))
            {
                data.StudentDateOfBirth = studentDob;
            }
            else
            {
                _logger.LogWarning("Failed to parse student date of birth: {DateText} for student: {Username}",
                    studentDateOfBirthText, studentUsername);
            }
        }

        if (!string.IsNullOrEmpty(parentDateOfBirthText))
        {
            if (TryParseDateSafely(parentDateOfBirthText, out DateTime parentDob))
            {
                data.ParentDateOfBirth = parentDob;
            }
            else
            {
                _logger.LogWarning("Failed to parse parent date of birth: {DateText} for parent: {PhoneNumber}",
                    parentDateOfBirthText, parentPhoneNumber);
            }
        }

        ValidateStudentParentCombinedData(data, studentDateOfBirthText, parentDateOfBirthText);
        return data;
    }

    private void ParseClassInformation(StudentParentCombinedExcelModel data, string classInfo)
    {
        try
        {
            var classEntries = classInfo.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var classEntry in classEntries)
            {
                var trimmedEntry = classEntry.Trim();
                if (string.IsNullOrEmpty(trimmedEntry)) continue;

                var parts = trimmedEntry.Split('|');

                if (parts.Length == 3)
                {
                    var className = parts[0].Trim();
                    var gradeText = parts[1].Trim();
                    var yearText = parts[2].Trim();

                    var classInfoItem = new ClassInfo { Name = className };

                    var errors = new List<string>();

                    if (string.IsNullOrEmpty(className))
                    {
                        errors.Add("Tên lớp không được để trống");
                    }

                    if (int.TryParse(gradeText, out int grade))
                    {
                        if (grade >= 1 && grade <= 12)
                        {
                            classInfoItem.Grade = grade;
                        }
                        else
                        {
                            errors.Add($"Khối {grade} không hợp lệ (phải từ 1-12)");
                        }
                    }
                    else
                    {
                        errors.Add($"Khối '{gradeText}' không phải số");
                    }

                    if (int.TryParse(yearText, out int academicYear))
                    {
                        var currentYear = DateTime.Now.Year;
                        if (academicYear >= currentYear - 2 && academicYear <= currentYear + 2)
                        {
                            classInfoItem.AcademicYear = academicYear;
                        }
                        else
                        {
                            errors.Add($"Năm học {academicYear} không hợp lệ");
                        }
                    }
                    else
                    {
                        errors.Add($"Năm học '{yearText}' không phải số");
                    }

                    classInfoItem.IsValid = !errors.Any();
                    classInfoItem.ErrorMessage = string.Join("; ", errors);

                    data.ClassInfoList.Add(classInfoItem);

                    data.ClassList.Add($"{className} (Khối {grade}, {academicYear})");
                }
                else
                {
                    data.ClassInfoList.Add(new ClassInfo
                    {
                        Name = trimmedEntry,
                        IsValid = false,
                        ErrorMessage = "Định dạng không đúng. Phải là: TênLớp|Khối|NămHọc"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            data.ClassInfoList.Add(new ClassInfo
            {
                Name = classInfo,
                IsValid = false,
                ErrorMessage = $"Lỗi phân tích thông tin lớp: {ex.Message}"
            });
        }
    }

    private void ValidateStudentParentCombinedData(StudentParentCombinedExcelModel data,
        string studentDateOfBirthText, string parentDateOfBirthText)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(data.StudentUsername))
            errors.Add("Tên đăng nhập học sinh không được để trống");

        if (string.IsNullOrEmpty(data.StudentEmail))
            errors.Add("Email học sinh không được để trống");
        else if (!IsValidEmail(data.StudentEmail))
            errors.Add("Email học sinh không đúng định dạng");

        if (string.IsNullOrEmpty(data.StudentFullName))
            errors.Add("Họ tên học sinh không được để trống");

        if (string.IsNullOrEmpty(data.StudentCode))
            errors.Add("Mã học sinh không được để trống");

        if (string.IsNullOrEmpty(data.StudentPhoneNumber))
        {
            errors.Add("Số điện thoại học sinh không được để trống");
        }
        else if (data.StudentPhoneNumber.Length < 10 || data.StudentPhoneNumber.Length > 11)
        {
            errors.Add("Số điện thoại học sinh không hợp lệ (phải có 10-11 số)");
        }

        if (!string.IsNullOrEmpty(studentDateOfBirthText))
        {
            if (data.StudentDateOfBirth.HasValue)
            {
                var age = DateTime.Now.Year - data.StudentDateOfBirth.Value.Year;
                if (data.StudentDateOfBirth.Value > DateTime.Now.AddYears(-age)) age--;

                if (age < 5 || age > 20)
                    errors.Add($"Tuổi học sinh phải từ 5 đến 20 (hiện tại: {age} tuổi)");
            }
            else
            {
                errors.Add($"Ngày sinh học sinh không đúng định dạng. Nhập: '{studentDateOfBirthText}'. " +
                           "Định dạng chấp nhận: dd/MM/yyyy (ví dụ: 15/03/2015)");
            }
        }

        if (!string.IsNullOrEmpty(data.StudentGender) &&
            !new[] { "Male", "Female", "Other" }.Contains(data.StudentGender))
            errors.Add("Giới tính học sinh phải là Male, Female hoặc Other");

        if (data.ClassInfoList.Any())
        {
            var invalidClasses = data.ClassInfoList.Where(c => !c.IsValid).ToList();
            if (invalidClasses.Any())
            {
                foreach (var invalidClass in invalidClasses)
                {
                    errors.Add($"Lớp '{invalidClass.Name}': {invalidClass.ErrorMessage}");
                }
            }
        }

        var hasAnyParentInfo = !string.IsNullOrWhiteSpace(data.ParentFullName) ||
                               !string.IsNullOrWhiteSpace(data.ParentPhoneNumber) ||
                               !string.IsNullOrWhiteSpace(data.ParentEmail) ||
                               !string.IsNullOrWhiteSpace(data.ParentAddress) ||
                               !string.IsNullOrWhiteSpace(data.ParentGender) ||
                               !string.IsNullOrWhiteSpace(parentDateOfBirthText) ||
                               !string.IsNullOrWhiteSpace(data.ParentRelationship);

        if (hasAnyParentInfo)
        {
            if (string.IsNullOrWhiteSpace(data.ParentFullName))
                errors.Add("Họ tên phụ huynh không được để trống (nếu cung cấp thông tin phụ huynh)");

            if (string.IsNullOrWhiteSpace(data.ParentPhoneNumber))
                errors.Add("Số điện thoại phụ huynh không được để trống (dùng để nhận dạng phụ huynh)");
            else if (data.ParentPhoneNumber.Length < 10 || data.ParentPhoneNumber.Length > 11)
                errors.Add("Số điện thoại phụ huynh không hợp lệ (phải có 10-11 số)");

            if (string.IsNullOrWhiteSpace(data.ParentEmail))
                errors.Add("Email phụ huynh không được để trống (cần để tạo tài khoản)");
            else if (!IsValidEmail(data.ParentEmail))
                errors.Add("Email phụ huynh không đúng định dạng");

            if (!string.IsNullOrEmpty(parentDateOfBirthText))
            {
                if (data.ParentDateOfBirth.HasValue)
                {
                    var age = DateTime.Now.Year - data.ParentDateOfBirth.Value.Year;
                    if (data.ParentDateOfBirth.Value > DateTime.Now.AddYears(-age)) age--;

                    if (age < 18)
                        errors.Add($"Tuổi phụ huynh phải từ 18 trở lên (hiện tại: {age} tuổi)");
                }
                else
                {
                    errors.Add($"Ngày sinh phụ huynh không đúng định dạng. Nhập: '{parentDateOfBirthText}'. " +
                               "Định dạng chấp nhận: dd/MM/yyyy (ví dụ: 20/05/1990)");
                }
            }

            if (!string.IsNullOrEmpty(data.ParentGender) &&
                !new[] { "Male", "Female", "Other" }.Contains(data.ParentGender))
                errors.Add("Giới tính phụ huynh phải là Male, Female hoặc Other");

            if (!string.IsNullOrEmpty(data.ParentRelationship) &&
                !new[] { "Father", "Mother", "Guardian" }.Contains(data.ParentRelationship))
                errors.Add("Mối quan hệ phải là: Father, Mother, Guardian");

            if (data.StudentPhoneNumber == data.ParentPhoneNumber)
            {
                data.LinkageType = "Cùng SĐT";
            }
            else
            {
                data.LinkageType = "Liên kết hàng ngang";
            }
        }
        else
        {
            data.LinkageType = "Chưa có phụ huynh";
        }

        data.IsValid = !errors.Any();
        data.ErrorMessage = string.Join("; ", errors);
    }

    private async Task CreateStudentParentCombinedTemplateSheet(ExcelWorksheet worksheet)
    {
        var headers = new[]
        {
            "Tên ĐN Học Sinh", "Email Học Sinh", "Họ Tên Học Sinh", "SĐT Học Sinh", "Địa Chỉ HS",
            "Giới Tính HS", "Ngày Sinh HS", "Mã Học Sinh", "Thông Tin Lớp",

            "Họ Tên Phụ Huynh", "SĐT Phụ Huynh", "Email Phụ Huynh", "Địa Chỉ PH",
            "Giới Tính PH", "Ngày Sinh PH", "Mối Quan Hệ"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        worksheet.Cells[2, 1].Value = "student001";
        worksheet.Cells[2, 2].Value = "student001@school.edu.vn";
        worksheet.Cells[2, 3].Value = "Nguyễn Văn A";
        worksheet.Cells[2, 4].Value = "0901234567";
        worksheet.Cells[2, 5].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[2, 6].Value = "Male";
        worksheet.Cells[2, 7].Value = "15/03/2015";
        worksheet.Cells[2, 8].Value = "HS001";
        worksheet.Cells[2, 9].Value = "3A|3|2024, 4B|4|2025, 5A|5|2026";
        worksheet.Cells[2, 10].Value = "Nguyễn Thị B";
        worksheet.Cells[2, 11].Value = "0901234567";
        worksheet.Cells[2, 12].Value = "parent001@gmail.com";
        worksheet.Cells[2, 13].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[2, 14].Value = "Female";
        worksheet.Cells[2, 15].Value = "20/05/1985";
        worksheet.Cells[2, 16].Value = "Mother";

        worksheet.Cells[3, 1].Value = "student002";
        worksheet.Cells[3, 2].Value = "student002@school.edu.vn";
        worksheet.Cells[3, 3].Value = "Nguyễn Thị C";
        worksheet.Cells[3, 4].Value = "0901234567";
        worksheet.Cells[3, 5].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[3, 6].Value = "Female";
        worksheet.Cells[3, 7].Value = "10/08/2017";
        worksheet.Cells[3, 8].Value = "HS002";
        worksheet.Cells[3, 9].Value = "2A|2|2026";
        worksheet.Cells[3, 10].Value = "Nguyễn Thị B";
        worksheet.Cells[3, 11].Value = "0901234567";
        worksheet.Cells[3, 12].Value = "parent001@gmail.com";
        worksheet.Cells[3, 13].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[3, 14].Value = "Female";
        worksheet.Cells[3, 15].Value = "20/05/1985";
        worksheet.Cells[3, 16].Value = "Mother";

        worksheet.Cells[4, 1].Value = "student003";
        worksheet.Cells[4, 2].Value = "student003@school.edu.vn";
        worksheet.Cells[4, 3].Value = "Nguyễn Văn D";
        worksheet.Cells[4, 4].Value = "0987654321";
        worksheet.Cells[4, 5].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[4, 6].Value = "Male";
        worksheet.Cells[4, 7].Value = "05/12/2019";
        worksheet.Cells[4, 8].Value = "HS003";
        worksheet.Cells[4, 9].Value = "1A|1|2026";
        worksheet.Cells[4, 10].Value = "Nguyễn Thị B";
        worksheet.Cells[4, 11].Value = "0901234567";
        worksheet.Cells[4, 12].Value = "parent001@gmail.com";
        worksheet.Cells[4, 13].Value = "123 Đường ABC, Quận 1, TP.HCM";
        worksheet.Cells[4, 14].Value = "Female";
        worksheet.Cells[4, 15].Value = "20/05/1985";
        worksheet.Cells[4, 16].Value = "Mother";

        worksheet.Cells[5, 1].Value = "student004";
        worksheet.Cells[5, 2].Value = "student004@school.edu.vn";
        worksheet.Cells[5, 3].Value = "Trần Văn E";
        worksheet.Cells[5, 4].Value = "0912345678";
        worksheet.Cells[5, 5].Value = "456 Đường XYZ, Quận 2, TP.HCM";
        worksheet.Cells[5, 6].Value = "Male";
        worksheet.Cells[5, 7].Value = "20/07/2016";
        worksheet.Cells[5, 8].Value = "HS004";
        worksheet.Cells[5, 9].Value = "3A|3|2026";
        worksheet.Cells[5, 10].Value = "";
        worksheet.Cells[5, 11].Value = "";
        worksheet.Cells[5, 12].Value = "";
        worksheet.Cells[5, 13].Value = "";
        worksheet.Cells[5, 14].Value = "";
        worksheet.Cells[5, 15].Value = "";
        worksheet.Cells[5, 16].Value = "";

        using (var range = worksheet.Cells[2, 1, 4, headers.Length])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            range.Style.Font.Italic = true;
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        using (var range = worksheet.Cells[5, 1, 5, headers.Length])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
            range.Style.Font.Italic = true;
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        using (var parentRange = worksheet.Cells[2, 10, 4, 16])
        {
            parentRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            parentRange.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
        }

        worksheet.Cells.AutoFitColumns();

        var classLogicComment = "LOGIC LỚP CHÍNH - VÍ DỤ:\n" +
                                "Dòng 2: 3A|3|2024, 4B|4|2025, 5A|5|2026\n" +
                                "→ Lớp chính: 5A (khối 5 cao nhất)\n" +
                                "→ Hiển thị: 'Học sinh đang học lớp 5A, Khối 5'\n" +
                                "→ HS có lịch sử học tập từ khối 3 đến 5\n\n" +
                                "KHUYẾN NGHỊ CHO MANAGER:\n" +
                                "- Import học sinh MỚI: chỉ nên 1 lớp hiện tại\n" +
                                "- Ví dụ: 2A|2|2026 (chỉ lớp đang học)\n" +
                                "- Lịch sử/tương lai: quản lý qua chức năng khác";

        worksheet.Cells[2, 9].AddComment(classLogicComment, "System");

        var bestPracticeComment = "CÁCH LÀM TỐT NHẤT:\n" +
                                  "✓ Chỉ nhập 1 lớp hiện tại: 2A|2|2026\n" +
                                  "✓ Đơn giản, rõ ràng, không nhầm lẫn\n" +
                                  "✓ Dễ quản lý và theo dõi\n" +
                                  "✓ Thêm lịch sử/tương lai sau qua giao diện\n\n" +
                                  "❌ Tránh: nhập nhiều lớp cùng lúc\n" +
                                  "❌ Có thể gây nhầm lẫn về lớp hiện tại";

        worksheet.Cells[3, 9].AddComment(bestPracticeComment, "System");

        var noParentComment = "Học sinh KHÔNG CÓ phụ huynh:\n" +
                              "- Để trống TẤT CẢ thông tin phụ huynh (cột J-P)\n" +
                              "- Học sinh sẽ được tạo mà không liên kết với phụ huynh nào\n" +
                              "- Có thể liên kết phụ huynh sau bằng chức năng khác\n" +
                              "- Màu hồng = không có phụ huynh";

        worksheet.Cells[5, 1].AddComment(noParentComment, "System");

        var phoneComment = "SĐT Học Sinh - LOGIC MỚI:\n" +
                           "✓ ĐƯỢC PHÉP trùng với SĐT phụ huynh\n" +
                           "✓ ĐƯỢC PHÉP nhiều học sinh cùng SĐT\n" +
                           "- Dòng 2,3: cùng SĐT phụ huynh\n" +
                           "- Dòng 4: SĐT riêng nhưng cùng phụ huynh\n" +
                           "- Liên kết theo hàng ngang (không phụ thuộc SĐT)";

        worksheet.Cells[2, 4].AddComment(phoneComment, "System");

        var multiClassComment = "CÁC VÍ DỤ LỚP:\n" +
                                "Dòng 2: Có lịch sử → Lớp chính = 5A (khối cao nhất)\n" +
                                "Dòng 3: Học sinh mới → Lớp chính = 2A (KHUYẾN NGHỊ)\n" +
                                "Dòng 4: Học sinh mới → Lớp chính = 1A (KHUYẾN NGHỊ)\n" +
                                "Dòng 5: Không có PH → Lớp chính = 3A\n\n" +
                                "⚠️ XÓA TẤT CẢ dữ liệu mẫu trước khi import!\n" +
                                "⚠️ Import học sinh mới: chỉ nên 1 lớp hiện tại";

        worksheet.Cells[4, 9].AddComment(multiClassComment, "System");

        await Task.CompletedTask;
    }

    private async Task CreateStudentParentCombinedInstructionSheet(ExcelWorksheet worksheet)
    {
        var currentYear = DateTime.Now.Year;
        var instructions = new[]
        {
            "HƯỚNG DẪN IMPORT HỌC SINH & PHỤ HUYNH KẾT HỢP (CÓ LỚP HỌC)",
            "",
            "⚠️ QUAN TRỌNG: PHẢI XÓA TẤT CẢ DỮ LIỆU MẪU TRƯỚC KHI IMPORT:",
            "   - Xóa các dòng mẫu từ dòng 2-5 trong sheet 'Template_HocSinh_PhuHuynh'",
            "   - Chỉ giữ lại dòng tiêu đề (dòng 1)",
            "   - Bắt đầu nhập dữ liệu thật từ dòng 2",
            "   - Nếu không xóa dữ liệu mẫu, hệ thống sẽ cố tạo tài khoản với dữ liệu giả",
            "",
            "1. NGUYÊN TẮC HOẠT ĐỘNG:",
            "   - Hệ thống sẽ tự động tạo phụ huynh dựa trên SĐT phụ huynh",
            "   - Nếu SĐT phụ huynh đã tồn tại → sử dụng phụ huynh có sẵn",
            "   - Nếu SĐT phụ huynh chưa tồn tại → tạo phụ huynh mới",
            "   - Sau đó tự động liên kết học sinh với phụ huynh THEO HÀNG NGANG",
            "   - Tự động add học sinh vào các lớp học được chỉ định",
            "   - ★ HỖ TRỢ: Học sinh KHÔNG CÓ phụ huynh (để trống thông tin PH)",
            "",
            "2. CẤU TRÚC FILE:",
            "   THÔNG TIN HỌC SINH (Cột A-I):",
            "   - Cột A: Tên Đăng Nhập (bắt buộc, duy nhất)",
            "   - Cột B: Email Học Sinh (bắt buộc, duy nhất)",
            "   - Cột C: Họ Tên Học Sinh (bắt buộc)",
            "   - Cột D: SĐT Học Sinh (bắt buộc, 10-11 số, có thể trùng với SĐT phụ huynh)",
            "   - Cột E: Địa Chỉ HS (tùy chọn)",
            "   - Cột F: Giới Tính HS (Male/Female/Other)",
            "   - Cột G: Ngày Sinh HS (dd/MM/yyyy, tuổi 5-20)",
            "   - Cột H: Mã Học Sinh (bắt buộc, duy nhất)",
            "   - Cột I: Thông Tin Lớp (định dạng đặc biệt - xem mục 4)",
            "",
            "   THÔNG TIN PHỤ HUYNH (Cột J-P) - TÙY CHỌN:",
            "   - Cột J: Họ Tên Phụ Huynh (bắt buộc nếu có PH)",
            "   - Cột K: SĐT Phụ Huynh (bắt buộc nếu có PH, dùng để nhận dạng)",
            "   - Cột L: Email Phụ Huynh (bắt buộc nếu có PH)",
            "   - Cột M: Địa Chỉ PH (tùy chọn)",
            "   - Cột N: Giới Tính PH (Male/Female/Other)",
            "   - Cột O: Ngày Sinh PH (dd/MM/yyyy, tuổi >= 18)",
            "   - Cột P: Mối Quan Hệ (Father/Mother/Guardian)",
            "",
            "3. HỌC SINH KHÔNG CÓ PHỤ HUYNH:",
            "   ★ ĐỂ TRỐNG TẤT CẢ thông tin phụ huynh (cột J-P) = Học sinh không có PH",
            "   ★ Chỉ cần điền đầy đủ thông tin học sinh (cột A-I)",
            "   ★ Hệ thống sẽ tạo học sinh độc lập, không liên kết với ai",
            "   ★ Có thể liên kết phụ huynh sau bằng chức năng khác",
            "",
            "4. LOGIC LIÊN KẾT PHỤ HUYNH-HỌC SINH:",
            "   ★ QUY TẮC: Tất cả học sinh sẽ được liên kết với phụ huynh CÙNG HÀNG NGANG",
            "   ★ KHÔNG QUAN TÂM SĐT có giống nhau hay không",
            "   ",
            "   CÁC TRƯỜNG HỢP ĐƯỢC HỖ TRỢ:",
            "   - TH1: SĐT học sinh = SĐT phụ huynh → Con dùng SĐT bố/mẹ (bình thường)",
            "   - TH2: SĐT học sinh ≠ SĐT phụ huynh → Con có SĐT riêng (bình thường)",
            "   - TH3: Nhiều học sinh cùng SĐT phụ huynh → Nhiều con dùng chung SĐT bố/mẹ",
            "   - TH4: Cùng SĐT phụ huynh nhưng khác hàng → Tái sử dụng phụ huynh đã tồn tại",
            "   - TH5: Không có thông tin phụ huynh → Học sinh độc lập",
            "",
            "5. ĐỊNH DẠNG THÔNG TIN LỚP (Cột I):",
            "   - Một lớp: TênLớp|Khối|NămHọc",
            "     Ví dụ: 5A|5|2026",
            "   - Nhiều lớp: phân cách bằng dấu phẩy",
            "     Ví dụ: 5A|5|2026, 6A|6|2025, 2A|2|2026",
            "   - Khối: số từ 1 đến 12",
            $"   - Năm học: từ {currentYear - 2} đến {currentYear + 2}",
            "   - Tên lớp phải tồn tại trong hệ thống",
            "",
            "   ★ QUAN TRỌNG - LỚP CHÍNH CỦA HỌC SINH:",
            "   - Hệ thống sẽ tự động chọn KHỐI CAO NHẤT làm lớp chính",
            "   - Nếu cùng khối cao nhất → chọn lớp có ngày thêm gần nhất",
            "   - Lớp chính = lớp học sinh đang học hiện tại",
            "   - Các lớp khác = lịch sử học tập trước đó",
            "",
            "   VÍ DỤ THỰC TẾ:",
            "   - Input: 3A|3|2024, 5A|5|2026, 4B|4|2025",
            "   - Kết quả: Lớp chính = 5A (khối 5 cao nhất)",
            "   - Hiển thị: 'Học sinh đang học lớp 5A, Khối 5'",
            "   - Logic: HS đã học 3A (2024), 4B (2025), hiện tại 5A (2026)",
            "",
            "   KHUYẾN NGHỊ CHO MANAGER:",
            "   - Import học sinh MỚI: chỉ nên nhập 1 lớp hiện tại",
            "   - Lịch sử/tương lai: thêm qua chức năng quản lý lớp sau",
            "   - Tránh nhập nhiều lớp cùng lúc để không gây nhầm lẫn",
            "",
            "6. QUY TẮC VALIDATION:",
            "   ★ KHÔNG CẤM trùng SĐT học sinh nữa",
            "   ★ CHO PHÉP nhiều học sinh dùng cùng SĐT phụ huynh",
            "   ★ CHO PHÉP học sinh KHÔNG CÓ phụ huynh",
            "   ",
            "   CÁC QUY TẮC CÒN LẠI:",
            "   - Tên đăng nhập học sinh không trùng với hệ thống",
            "   - Email học sinh không trùng với hệ thống",
            "   - Mã học sinh không trùng với hệ thống",
            "   - SĐT học sinh bắt buộc (10-11 số)",
            "   - Nếu có phụ huynh: SĐT, Email, Họ tên PH bắt buộc",
            "   - Email phụ huynh không trùng (nếu tạo mới)",
            "   - Tuổi học sinh: 5-20 tuổi",
            "   - Tuổi phụ huynh: >= 18 tuổi",
            "   - Lớp học phải tồn tại trong hệ thống",
            "",
            "7. CÁCH THỰC HIỆN IMPORT:",
            "   BƯỚC 1: Tải template về máy",
            "   BƯỚC 2: ⚠️ XÓA TẤT CẢ dữ liệu mẫu (dòng 2-5)",
            "   BƯỚC 3: Nhập dữ liệu thật từ dòng 2 trở đi",
            "   BƯỚC 4: Kiểm tra lại định dạng ngày tháng",
            "   BƯỚC 5: Lưu file và upload lên hệ thống",
            "",
            "8. VÍ DỤ THỰC TẾ - NHIỀU LỚP:",
            "   TRƯỜNG HỢP 1: Học sinh có lịch sử và hiện tại",
            "   - Input: 3A|3|2024, 4B|4|2025, 5A|5|2026",
            "   - Kết quả: Lớp chính = 5A (khối 5), Hiện đang học lớp 5A",
            "   - Ý nghĩa: HS đã học 3A, 4B, hiện tại học 5A",
            "",
            "   TRƯỜNG HỢP 2: Manager import học sinh mới và tạo kế hoạch",
            "   - Input: 1A|1|2026 (lớp hiện tại duy nhất)",
            "   - Kết quả: Lớp chính = 1A (khối 1), Hiện đang học lớp 1A",
            "   - Sau đó: Manager có thể thêm lớp tương lai qua chức năng khác",
            "   - Lưu ý: Không nên import nhiều lớp tương lai cùng lúc",
            "",
            "   TRƯỜNG HỢP 3: Import với lịch sử (ít gặp)",
            "   - Input: 2A|2|2024, 3B|3|2025, 4A|4|2026",
            "   - Kết quả: Lớp chính = 4A (khối 4), Hiện đang học lớp 4A",
            "   - Ý nghĩa: Import học sinh đã có lịch sử học tập",
            "",
            "   TRƯỜNG HỢP 4: Cùng khối cao nhất",
            "   - Input: 5A|5|2026, 5B|5|2026 (cùng ngày tạo)",
            "   - Kết quả: Lớp chính = lớp được thêm sau cùng",
            "   - Lưu ý: Nên tránh trường hợp này, chỉ nên 1 lớp/khối",
            "",
            "   VÍ DỤ LIÊN KẾT PHỤ HUYNH:",
            "   Dòng 2: student001, email1@..., Nguyễn Văn A, 0901234567, ..., 5A|5|2026, Nguyễn Thị B, 0901234567",
            "   Dòng 3: student002, email2@..., Nguyễn Thị C, 0901234567, ..., 2A|2|2026, Nguyễn Thị B, 0901234567",
            "   Dòng 4: student003, email3@..., Nguyễn Văn D, 0987654321, ..., 1A|1|2026, [TẤT CẢ TRỐNG]",
            "   ➜ Kết quả: 2 học sinh đầu cùng phụ huynh B, học sinh thứ 3 không có phụ huynh",
            "   ➜ Lớp chính: A học 5A, C học 2A, D học 1A",
            "",
            "9. CÁC LỖI THƯỜNG GẶP:",
            "   - QUÊN XÓA dữ liệu mẫu → Hệ thống cố tạo user với data giả",
            "   - Thiếu SĐT học sinh (cột D bắt buộc)",
            "   - Định dạng thông tin lớp sai (thiếu |, sai thứ tự)",
            "   - Lớp học không tồn tại trong hệ thống",
            "   - Khối hoặc năm học ngoài khoảng cho phép",
            "   - Username/Email/Mã học sinh trùng lặp",
            "   - Điền thiếu thông tin phụ huynh (nếu muốn có PH thì phải đầy đủ)",
            "   ✓ KHÔNG CÒN LỖI: Trùng SĐT học sinh",
            "",
            "10. SAU KHI IMPORT THÀNH CÔNG:",
            "    - Học sinh: nhận password qua email",
            "    - Phụ huynh mới: nhận password qua email",
            "    - Phụ huynh cũ: nhận thông báo có thêm con mới",
            "    - Học sinh được thêm vào các lớp được chỉ định",
            "    - Có thể xem báo cáo chi tiết kết quả import",
            "",
            "11. LƯU Ý QUAN TRỌNG:",
            "    ⚠️ LUÔN XÓA DỮ LIỆU MẪU TRƯỚC KHI IMPORT",
            "    ⚠️ Dữ liệu mẫu chỉ để tham khảo định dạng",
            "    ⚠️ Nếu import với dữ liệu mẫu sẽ tạo tài khoản giả",
            "    ★ Học sinh có thể tồn tại mà không cần phụ huynh",
            "    ★ Có thể liên kết phụ huynh sau bằng chức năng quản lý",
            "    ★ LỚP CHÍNH = KHỐI CAO NHẤT trong danh sách lớp",
            "    ★ KHUYẾN NGHỊ: Import học sinh mới chỉ với 1 lớp hiện tại",
            "    ★ Lịch sử/tương lai: quản lý qua chức năng chuyển lớp",
            ""
        };

        await CreateInstructionContent(worksheet, instructions);
    }

    private string GetDateValue(ExcelRange cell)
    {
        if (cell.Value == null) return "";

        try
        {
            if (cell.Value is DateTime dateValue)
            {
                return dateValue.ToString("dd/MM/yyyy");
            }

            if (cell.Value is double serialDate)
            {
                try
                {
                    var date = DateTime.FromOADate(serialDate);
                    return date.ToString("dd/MM/yyyy");
                }
                catch
                {
                    return TryParseTextDate(cell.Text?.Trim() ?? "");
                }
            }

            if (cell.Value is string stringValue)
            {
                return TryParseTextDate(stringValue.Trim());
            }

            return TryParseTextDate(cell.Text?.Trim() ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error parsing date from cell: {CellValue}, Error: {Error}",
                cell.Value?.ToString() ?? "null", ex.Message);
            return "";
        }
    }

    private string TryParseTextDate(string dateText)
    {
        if (string.IsNullOrWhiteSpace(dateText))
            return "";

        var dateFormats = new[]
        {
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd/M/yyyy",
            "d/MM/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "dd-M-yyyy",
            "d-MM-yyyy",
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "dd.MM.yyyy",
            "d.M.yyyy"
        };

        foreach (var format in dateFormats)
        {
            if (DateTime.TryParseExact(dateText, format, null, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.ToString("dd/MM/yyyy");
            }
        }

        if (DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime cultureDate))
        {
            return cultureDate.ToString("dd/MM/yyyy");
        }

        if (DateTime.TryParse(dateText, out DateTime currentCultureDate))
        {
            return currentCultureDate.ToString("dd/MM/yyyy");
        }

        return "";
    }

    private bool TryParseDateSafely(string dateText, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(dateText))
            return false;

        var dateFormats = new[]
        {
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd/M/yyyy",
            "d/MM/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "dd-M-yyyy",
            "d-MM-yyyy",
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "dd.MM.yyyy",
            "d.M.yyyy"
        };

        foreach (var format in dateFormats)
        {
            if (DateTime.TryParseExact(dateText, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        if (DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(dateText, out date))
        {
            return true;
        }

        return false;
    }

    private string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";

        return new string(phone.Where(char.IsDigit).ToArray());
    }

    #endregion
}
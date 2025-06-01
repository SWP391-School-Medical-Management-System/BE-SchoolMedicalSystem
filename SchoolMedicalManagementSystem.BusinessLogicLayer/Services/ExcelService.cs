using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System.Drawing;
using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

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

    #region Student Excel Operations

    public async Task<byte[]> GenerateStudentTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template_Student");
            var instructionSheet = package.Workbook.Worksheets.Add("Huong_Dan");

            await CreateStudentTemplateSheet(worksheet);
            await CreateStudentInstructionSheet(instructionSheet);

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating student template");
            throw;
        }
    }

    public async Task<ExcelImportResult<StudentExcelModel>> ReadStudentExcelAsync(IFormFile file)
    {
        var result = new ExcelImportResult<StudentExcelModel>();

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

            var students = new List<StudentExcelModel>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 3; row <= rowCount; row++)
            {
                var student = ReadStudentFromRow(worksheet, row);
                if (student != null)
                    students.Add(student);
            }

            result.TotalRows = students.Count;
            result.ValidData = students.Where(s => s.IsValid).ToList();
            result.InvalidData = students.Where(s => !s.IsValid).ToList();
            result.SuccessRows = result.ValidData.Count;
            result.ErrorRows = result.InvalidData.Count;
            result.Success = result.TotalRows > 0;
            result.Message = result.Success ? "Đọc file Excel thành công." : "File Excel không có dữ liệu hợp lệ.";

            if (result.InvalidData.Any())
            {
                result.Errors = result.InvalidData.Select(s => $"Dòng {students.IndexOf(s) + 3}: {s.ErrorMessage}")
                    .ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading student Excel file");
            result.Success = false;
            result.Message = $"Lỗi đọc file Excel: {ex.Message}";
            return result;
        }
    }

    public async Task<byte[]> ExportStudentsToExcelAsync(List<StudentResponse> students)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh_Sach_Hoc_Sinh");

            var headers = new[]
            {
                "STT", "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
                "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mã Học Sinh",
                "Tổng Số Lớp", "Tất Cả Lớp", "Phụ Huynh", "Ngày Tạo"
            };

            CreateHeaderRow(worksheet, headers);

            for (int i = 0; i < students.Count; i++)
            {
                var student = students[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = student.Username;
                worksheet.Cells[row, 3].Value = student.Email;
                worksheet.Cells[row, 4].Value = student.FullName;
                worksheet.Cells[row, 5].Value = student.PhoneNumber;
                worksheet.Cells[row, 6].Value = student.Address;
                worksheet.Cells[row, 7].Value = student.Gender;
                worksheet.Cells[row, 8].Value = student.DateOfBirth?.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 9].Value = student.StudentCode;

                worksheet.Cells[row, 10].Value = student.ClassCount;

                var allClasses = student.Classes?.Select(c => $"{c.ClassName} ({c.AcademicYear})")
                    .ToList() ?? new List<string>();
                worksheet.Cells[row, 11].Value = string.Join("; ", allClasses);

                worksheet.Cells[row, 12].Value = student.ParentName;
                worksheet.Cells[row, 13].Value = student.CreatedDate?.ToString("dd/MM/yyyy HH:mm");
            }

            worksheet.Cells.AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting students to Excel");
            throw;
        }
    }

    #endregion

    #region Parent Excel Operations

    public async Task<byte[]> GenerateParentTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template_Parent");
            var instructionSheet = package.Workbook.Worksheets.Add("Huong_Dan");

            await CreateParentTemplateSheet(worksheet);
            await CreateParentInstructionSheet(instructionSheet);

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating parent template");
            throw;
        }
    }

    public async Task<ExcelImportResult<ParentExcelModel>> ReadParentExcelAsync(IFormFile file)
    {
        var result = new ExcelImportResult<ParentExcelModel>();

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

            var parents = new List<ParentExcelModel>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 3; row <= rowCount; row++)
            {
                var parent = ReadParentFromRow(worksheet, row);
                if (parent != null)
                    parents.Add(parent);
            }

            result.TotalRows = parents.Count;
            result.ValidData = parents.Where(p => p.IsValid).ToList();
            result.InvalidData = parents.Where(p => !p.IsValid).ToList();
            result.SuccessRows = result.ValidData.Count;
            result.ErrorRows = result.InvalidData.Count;
            result.Success = result.TotalRows > 0;
            result.Message = result.Success ? "Đọc file Excel thành công." : "File Excel không có dữ liệu hợp lệ.";

            if (result.InvalidData.Any())
            {
                result.Errors = result.InvalidData.Select(p => $"Dòng {parents.IndexOf(p) + 3}: {p.ErrorMessage}")
                    .ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading parent Excel file");
            result.Success = false;
            result.Message = $"Lỗi đọc file Excel: {ex.Message}";
            return result;
        }
    }

    public async Task<byte[]> ExportParentsToExcelAsync(List<ParentResponse> parents)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh_Sach_Phu_Huynh");

            var headers = new[]
            {
                "STT", "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
                "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mối Quan Hệ", "Số Con", "Ngày Tạo"
            };

            CreateHeaderRow(worksheet, headers);

            for (int i = 0; i < parents.Count; i++)
            {
                var parent = parents[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = parent.Username;
                worksheet.Cells[row, 3].Value = parent.Email;
                worksheet.Cells[row, 4].Value = parent.FullName;
                worksheet.Cells[row, 5].Value = parent.PhoneNumber;
                worksheet.Cells[row, 6].Value = parent.Address;
                worksheet.Cells[row, 7].Value = parent.Gender;
                worksheet.Cells[row, 8].Value = parent.DateOfBirth?.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 9].Value = parent.Relationship;
                worksheet.Cells[row, 10].Value = parent.ChildrenCount;
                worksheet.Cells[row, 11].Value = parent.CreatedDate?.ToString("dd/MM/yyyy HH:mm");
            }

            worksheet.Cells.AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting parents to Excel");
            throw;
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

    private StudentExcelModel ReadStudentFromRow(ExcelWorksheet worksheet, int row)
    {
        var username = worksheet.Cells[row, 1].Text?.Trim();
        var email = worksheet.Cells[row, 2].Text?.Trim();
        var fullName = worksheet.Cells[row, 3].Text?.Trim();
        var phoneNumber = worksheet.Cells[row, 4].Text?.Trim();
        var address = worksheet.Cells[row, 5].Text?.Trim();
        var gender = worksheet.Cells[row, 6].Text?.Trim();
        var dateOfBirthText = "";
        var dateCell = worksheet.Cells[row, 7];
        var studentCode = worksheet.Cells[row, 8].Text?.Trim();
        var classNames = worksheet.Cells[row, 9].Text?.Trim();

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

        var student = new StudentExcelModel
        {
            Username = username,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Address = address,
            Gender = gender,
            StudentCode = studentCode,
            ClassNames = classNames
        };

        if (!string.IsNullOrEmpty(classNames))
        {
            student.ClassList = classNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
        }

        ValidateStudentData(student, dateOfBirthText);
        return student;
    }

    private void ValidateStudentData(StudentExcelModel student, string dateOfBirthText)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(student.Username))
            errors.Add("Tên đăng nhập không được để trống");

        if (string.IsNullOrEmpty(student.Email))
            errors.Add("Email không được để trống");
        else if (!IsValidEmail(student.Email))
            errors.Add("Email không đúng định dạng");

        if (string.IsNullOrEmpty(student.FullName))
            errors.Add("Họ tên không được để trống");

        if (string.IsNullOrEmpty(student.StudentCode))
            errors.Add("Mã học sinh không được để trống");

        if (!string.IsNullOrEmpty(dateOfBirthText))
        {
            if (DateTime.TryParse(dateOfBirthText, out DateTime dateOfBirth))
            {
                student.DateOfBirth = dateOfBirth;
                if (dateOfBirth > DateTime.Now.AddYears(-5) || dateOfBirth < DateTime.Now.AddYears(-20))
                    errors.Add("Tuổi học sinh phải từ 5 đến 20");
            }
            else
            {
                errors.Add("Ngày sinh không đúng định dạng");
            }
        }

        if (!string.IsNullOrEmpty(student.Gender) &&
            !new[] { "Male", "Female", "Other" }.Contains(student.Gender))
            errors.Add("Giới tính phải là Male/Female hoặc Other");

        student.IsValid = !errors.Any();
        student.ErrorMessage = string.Join("; ", errors);
    }

    private ParentExcelModel ReadParentFromRow(ExcelWorksheet worksheet, int row)
    {
        var username = worksheet.Cells[row, 1].Text?.Trim();
        var email = worksheet.Cells[row, 2].Text?.Trim();
        var fullName = worksheet.Cells[row, 3].Text?.Trim();
        var phoneNumber = worksheet.Cells[row, 4].Text?.Trim();
        var address = worksheet.Cells[row, 5].Text?.Trim();
        var gender = worksheet.Cells[row, 6].Text?.Trim();
        var dateOfBirthText = "";
        var dateCell = worksheet.Cells[row, 7];
        var relationship = worksheet.Cells[row, 8].Text?.Trim();

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

        var parent = new ParentExcelModel
        {
            Username = username,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Address = address,
            Gender = gender,
            Relationship = relationship
        };

        ValidateParentData(parent, dateOfBirthText);
        return parent;
    }

    private void ValidateParentData(ParentExcelModel parent, string dateOfBirthText)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(parent.Username))
            errors.Add("Tên đăng nhập không được để trống");

        if (string.IsNullOrEmpty(parent.Email))
            errors.Add("Email không được để trống");
        else if (!IsValidEmail(parent.Email))
            errors.Add("Email không đúng định dạng");

        if (string.IsNullOrEmpty(parent.FullName))
            errors.Add("Họ tên không được để trống");

        if (!string.IsNullOrEmpty(dateOfBirthText))
        {
            if (DateTime.TryParse(dateOfBirthText, out DateTime dateOfBirth))
            {
                parent.DateOfBirth = dateOfBirth;
                if (dateOfBirth > DateTime.Now.AddYears(-18))
                    errors.Add("Tuổi phải từ 18 trở lên");
            }
            else
            {
                errors.Add("Ngày sinh không đúng định dạng");
            }
        }

        if (!string.IsNullOrEmpty(parent.Gender) &&
            !new[] { "Male", "Female", "Other" }.Contains(parent.Gender))
            errors.Add("Giới tính phải là Male/Female hoặc Other");

        if (!string.IsNullOrEmpty(parent.Relationship) &&
            !new[] { "Father", "Mother", "Guardian" }.Contains(parent.Relationship))
            errors.Add("Mối quan hệ phải là: Father, Mother, Guardian");

        parent.IsValid = !errors.Any();
        parent.ErrorMessage = string.Join("; ", errors);
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

    private async Task CreateStudentTemplateSheet(ExcelWorksheet worksheet)
    {
        var headers = new[]
        {
            "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
            "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mã Học Sinh"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        worksheet.Cells[2, 1].Value = "student001";
        worksheet.Cells[2, 2].Value = "student001@school.edu.vn";
        worksheet.Cells[2, 3].Value = "Lê Văn C";
        worksheet.Cells[2, 4].Value = "0923456789";
        worksheet.Cells[2, 5].Value = "789 Đường GHI, Quận 3, TP.HCM";
        worksheet.Cells[2, 6].Value = "Male";
        worksheet.Cells[2, 7].Value = "10/03/2010";
        worksheet.Cells[2, 8].Value = "HS001";

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

    private async Task CreateParentTemplateSheet(ExcelWorksheet worksheet)
    {
        var headers = new[]
        {
            "Tên Đăng Nhập", "Email", "Họ Tên", "Số Điện Thoại",
            "Địa Chỉ", "Giới Tính", "Ngày Sinh", "Mối Quan Hệ"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        worksheet.Cells[2, 1].Value = "parent001";
        worksheet.Cells[2, 2].Value = "parent001@gmail.com";
        worksheet.Cells[2, 3].Value = "Phạm Thị D";
        worksheet.Cells[2, 4].Value = "0934567890";
        worksheet.Cells[2, 5].Value = "321 Đường JKL, Quận 4, TP.HCM";
        worksheet.Cells[2, 6].Value = "Female";
        worksheet.Cells[2, 7].Value = "20/07/1980";
        worksheet.Cells[2, 8].Value = "Mother";

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

    private async Task CreateStudentInstructionSheet(ExcelWorksheet worksheet)
    {
        var instructions = new[]
        {
            "HƯỚNG DẪN IMPORT STUDENT",
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
            "   - Cột G: Ngày Sinh (định dạng dd/MM/yyyy, tuổi 5-20)",
            "   - Cột H: Mã Học Sinh (bắt buộc, duy nhất)",
            "",
            "3. LƯU Ý:",
            "   - Hệ thống sẽ tự động tạo mật khẩu và gửi qua email",
            "   - Có thể liên kết với phụ huynh sau khi tạo tài khoản"
        };

        await CreateInstructionContent(worksheet, instructions);
    }

    private async Task CreateParentInstructionSheet(ExcelWorksheet worksheet)
    {
        var instructions = new[]
        {
            "HƯỚNG DẪN IMPORT PARENT",
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
            "   - Cột H: Mối Quan Hệ (Father/Mother/Guardian)",
            "",
            "3. LƯU Ý:",
            "   - Hệ thống sẽ tự động tạo mật khẩu và gửi qua email",
            "   - Có thể liên kết với học sinh sau khi tạo tài khoản"
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

    #endregion
}
using FluentValidation;
using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.SchoolClass;

public class ImportSchoolClassExcelValidator : AbstractValidator<ImportSchoolClassExcelRequest>
{
    public ImportSchoolClassExcelValidator()
    {
        RuleFor(x => x.ExcelFile)
            .NotNull()
            .WithMessage("File Excel là bắt buộc.");

        RuleFor(x => x.ExcelFile)
            .Must(BeValidExcelFile)
            .WithMessage("File phải có định dạng Excel (.xlsx hoặc .xls).")
            .When(x => x.ExcelFile != null);

        RuleFor(x => x.ExcelFile)
            .Must(BeValidFileSize)
            .WithMessage("Kích thước file không được vượt quá 10MB.")
            .When(x => x.ExcelFile != null);
    }

    private bool BeValidExcelFile(IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return allowedExtensions.Contains(extension);
    }

    private bool BeValidFileSize(IFormFile file)
    {
        if (file == null) return false;

        const long maxSize = 10 * 1024 * 1024; // 10MB
        return file.Length <= maxSize;
    }
}
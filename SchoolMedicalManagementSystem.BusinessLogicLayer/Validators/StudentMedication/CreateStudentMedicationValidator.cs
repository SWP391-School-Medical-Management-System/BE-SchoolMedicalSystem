using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;

public class CreateStudentMedicationValidator : AbstractValidator<CreateStudentMedicationRequest>
{
    public CreateStudentMedicationValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("ID học sinh không được để trống.");

        RuleFor(x => x.MedicationName)
            .NotEmpty()
            .WithMessage("Tên thuốc không được để trống.")
            .MaximumLength(200)
            .WithMessage("Tên thuốc không được vượt quá 200 ký tự.");

        RuleFor(x => x.Dosage)
            .NotEmpty()
            .WithMessage("Liều lượng không được để trống.")
            .MaximumLength(100)
            .WithMessage("Liều lượng không được vượt quá 100 ký tự.");

        RuleFor(x => x.Instructions)
            .NotEmpty()
            .WithMessage("Hướng dẫn sử dụng không được để trống.")
            .MaximumLength(1000)
            .WithMessage("Hướng dẫn sử dụng không được vượt quá 1000 ký tự.");

        RuleFor(x => x.Frequency)
            .NotEmpty()
            .WithMessage("Tần suất sử dụng không được để trống.")
            .MaximumLength(200)
            .WithMessage("Tần suất sử dụng không được vượt quá 200 ký tự.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Ngày bắt đầu không được để trống.")
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Ngày bắt đầu không được nhỏ hơn ngày hiện tại.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Ngày kết thúc không được để trống.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty()
            .WithMessage("Ngày hết hạn thuốc không được để trống.")
            .GreaterThan(DateTime.Today)
            .WithMessage("Thuốc đã hết hạn không thể gửi.")
            .GreaterThanOrEqualTo(x => x.EndDate)
            .WithMessage("Ngày hết hạn thuốc phải sau ngày kết thúc sử dụng.");

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .WithMessage("Mục đích sử dụng không được để trống.")
            .MaximumLength(500)
            .WithMessage("Mục đích sử dụng không được vượt quá 500 ký tự.");

        RuleFor(x => x.SideEffects)
            .MaximumLength(1000)
            .WithMessage("Tác dụng phụ không được vượt quá 1000 ký tự.");

        RuleFor(x => x.StorageInstructions)
            .MaximumLength(500)
            .WithMessage("Hướng dẫn bảo quản không được vượt quá 500 ký tự.");
    }
}
using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.MedicalItemUsage
{
    public class MedicalItemUsageRequestValidator : AbstractValidator<MedicalItemUsageRequest>
    {
        public MedicalItemUsageRequestValidator()
        {
            RuleFor(x => x.MedicalItemId)
                .NotEmpty()
                .WithMessage("ID thuốc/vật tư không được để trống.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Số lượng phải lớn hơn 0.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Notes));

            RuleFor(x => x.UsedAt)
                .NotEmpty()
                .WithMessage("Thời gian sử dụng không được để trống.")
                .LessThanOrEqualTo(DateTime.Now.AddHours(1))
                .WithMessage("Thời gian sử dụng không được trong tương lai.")
                .GreaterThan(DateTime.Now.AddDays(-90))
                .WithMessage("Thời gian sử dụng không được quá 90 ngày trước.");
        }
    }
}

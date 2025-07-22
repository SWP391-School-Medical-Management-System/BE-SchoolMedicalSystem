using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthCheck
{
    public class SaveVisionCheckRequestValidator : AbstractValidator<SaveVisionCheckRequest>
    {
        public SaveVisionCheckRequestValidator()
        {          
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("ID học sinh là bắt buộc.");
            RuleFor(x => x.HealthCheckItemId)
                .NotEmpty().WithMessage("ID hạng mục kiểm tra là bắt buộc.");
            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0).When(x => x.Value.HasValue).WithMessage("Kết quả thị lực phải lớn hơn hoặc bằng 0.");
        }
    }
}

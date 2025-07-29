using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthCheck
{
    public class SaveHearingCheckRequestValidator : AbstractValidator<SaveVisionCheckRequest>
    {
        public SaveHearingCheckRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("ID học sinh là bắt buộc.");
            RuleFor(x => x.HealthCheckItemId)
                .NotEmpty().WithMessage("ID hạng mục kiểm tra là bắt buộc.");
            RuleFor(x => x.Value)
                .Must(v => v == null || v >= 0).WithMessage("Kết quả thính lực phải lớn hơn hoặc bằng 0.");
        }          
    }
}

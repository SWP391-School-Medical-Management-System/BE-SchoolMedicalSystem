using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthCheck
{
    public class ParentApproveHealthCheckRequestValidator : AbstractValidator<ParentApproveHealthCheckRequest>
    {
        public ParentApproveHealthCheckRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Trạng thái là bắt buộc")
                .Must(status => status == "Confirmed" || status == "Declined").WithMessage("Trạng thái phải là 'Confirmed' hoặc 'Declined'");
        }
    }
}

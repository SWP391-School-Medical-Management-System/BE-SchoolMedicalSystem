using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthCheck
{
    public class AssignNurseToHealthCheckRequestValidator : AbstractValidator<AssignNurseToHealthCheckRequest>
    {
        public AssignNurseToHealthCheckRequestValidator()
        {
            RuleFor(x => x.HealthCheckId)
                .NotEmpty().WithMessage("ID buổi khám là bắt buộc");

            RuleFor(x => x.Assignments)
                .NotEmpty().WithMessage("Danh sách phân công là bắt buộc")
                .Must(x => x != null && x.Any()).WithMessage("Phải có ít nhất một phân công");

            RuleForEach(x => x.Assignments).ChildRules(assignment =>
            {
                assignment.RuleFor(a => a.HealthCheckItemId)
                    .NotEmpty().WithMessage("ID lớp học là bắt buộc");

                assignment.RuleFor(a => a.NurseId)
                    .NotEmpty().WithMessage("ID y tá là bắt buộc");
            });
        }
    }
}

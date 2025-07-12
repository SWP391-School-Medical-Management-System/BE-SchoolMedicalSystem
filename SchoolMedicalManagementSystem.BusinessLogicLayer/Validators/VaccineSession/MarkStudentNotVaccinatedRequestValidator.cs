using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession
{
    public class MarkStudentNotVaccinatedRequestValidator : AbstractValidator<MarkStudentNotVaccinatedRequest>
    {
        public MarkStudentNotVaccinatedRequestValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentId không được rỗng.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Lý do không tiêm là bắt buộc.");
        }
    }
}

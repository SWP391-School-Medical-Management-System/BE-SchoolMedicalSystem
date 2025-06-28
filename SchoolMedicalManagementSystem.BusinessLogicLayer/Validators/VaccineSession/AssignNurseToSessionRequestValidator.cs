using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession
{
    public class AssignNurseToSessionRequestValidator : AbstractValidator<AssignNurseToSessionRequest>
    {
        public AssignNurseToSessionRequestValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("SessionId không được để trống.");

            RuleFor(x => x.ClassId)
                .NotEmpty()
                .WithMessage("ClassId không được để trống.");

            RuleFor(x => x.NurseId)
                .NotEmpty()
                .WithMessage("NurseId không được để trống.");
        }
    }
}

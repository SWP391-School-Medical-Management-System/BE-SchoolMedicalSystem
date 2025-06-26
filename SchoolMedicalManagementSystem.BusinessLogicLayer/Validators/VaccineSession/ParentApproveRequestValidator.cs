using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession
{
    public class ParentApproveRequestValidator : AbstractValidator<ParentApproveRequest>
    {
        public ParentApproveRequestValidator()
        {

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status không được để trống.")
                .Must(status => status == "Confirmed" || status == "Declined")
                .WithMessage("Status chỉ được là 'Confirmed' hoặc 'Declined'.");
        }
    }
}

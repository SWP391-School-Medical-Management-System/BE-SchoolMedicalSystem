using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession
{
    public class UpdateStudentVaccinationResultRequestValidator : AbstractValidator<UpdateStudentVaccinationResultRequest>
    {
        public UpdateStudentVaccinationResultRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty()
                .WithMessage("ID học sinh là bắt buộc.");

            RuleFor(x => x.Symptoms)
                .MaximumLength(500).WithMessage("Triệu chứng không được vượt quá 500 ký tự.")
                .When(x => x.IsVaccinated && !string.IsNullOrEmpty(x.Symptoms));

            RuleFor(x => x.NoteAfterSession)
                .MaximumLength(1000).WithMessage("Ghi chú sau tiêm không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.NoteAfterSession));
        }
    }
}

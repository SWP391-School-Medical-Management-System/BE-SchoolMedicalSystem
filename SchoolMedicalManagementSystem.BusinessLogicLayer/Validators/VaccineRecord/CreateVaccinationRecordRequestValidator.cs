using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineRecord
{
    public class CreateVaccinationRecordRequestValidator : AbstractValidator<CreateVaccinationRecordRequest>
    {
        public CreateVaccinationRecordRequestValidator()
        {
            RuleFor(x => x.VaccinationTypeId)
                .NotEmpty()
                .WithMessage("ID loại vaccine là bắt buộc.");

            RuleFor(x => x.DoseNumber)
                .GreaterThan(0)
                .WithMessage("Số mũi tiêm phải lớn hơn 0.")
                .LessThanOrEqualTo(10)
                .WithMessage("Số mũi tiêm không được vượt quá 10.");

            RuleFor(x => x.AdministeredDate)
                .NotEmpty()
                .WithMessage("Ngày tiêm là bắt buộc.")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Ngày tiêm không được là tương lai.");

            RuleFor(x => x.AdministeredBy)
                .NotEmpty()
                .WithMessage("Người thực hiện tiêm là bắt buộc.")
                .MaximumLength(100)
                .WithMessage("Tên người thực hiện tiêm không được vượt quá 100 ký tự.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
        }
    }
}

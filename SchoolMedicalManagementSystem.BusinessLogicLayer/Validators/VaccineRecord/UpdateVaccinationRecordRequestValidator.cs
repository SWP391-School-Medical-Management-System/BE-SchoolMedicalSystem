using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineRecord
{
    public class UpdateVaccinationRecordRequestValidator : AbstractValidator<UpdateVaccinationRecordRequest>
    {
        public UpdateVaccinationRecordRequestValidator()
        {
            RuleFor(x => x.VaccinationTypeId)
                .NotEmpty()
                .When(x => x.VaccinationTypeId.HasValue)
                .WithMessage("ID loại vaccine là bắt buộc.");

            RuleFor(x => x.DoseNumber)
                .GreaterThan(0)
                .When(x => x.DoseNumber.HasValue)
                .WithMessage("Số mũi tiêm phải lớn hơn 0.")
                .LessThanOrEqualTo(10)
                .When(x => x.DoseNumber.HasValue)
                .WithMessage("Số mũi tiêm không được vượt quá 10.");

            RuleFor(x => x.AdministeredDate)
                .NotEmpty()
                .When(x => x.AdministeredDate.HasValue)
                .WithMessage("Ngày tiêm là bắt buộc.")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.AdministeredDate.HasValue)
                .WithMessage("Ngày tiêm không được là tương lai.");

            RuleFor(x => x.AdministeredBy)
                .NotEmpty()
                .When(x => x.AdministeredBy != null)
                .WithMessage("Người thực hiện tiêm là bắt buộc.")
                .MaximumLength(100)
                .When(x => x.AdministeredBy != null)
                .WithMessage("Tên người thực hiện tiêm không được vượt quá 100 ký tự.");

            RuleFor(x => x.BatchNumber)
                .NotEmpty()
                .When(x => x.BatchNumber != null)
                .WithMessage("Số lô vaccine là bắt buộc.")
                .MaximumLength(50)
                .When(x => x.BatchNumber != null)
                .WithMessage("Số lô vaccine không được vượt quá 50 ký tự.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .When(x => x.Notes != null)
                .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
        }
    }
}

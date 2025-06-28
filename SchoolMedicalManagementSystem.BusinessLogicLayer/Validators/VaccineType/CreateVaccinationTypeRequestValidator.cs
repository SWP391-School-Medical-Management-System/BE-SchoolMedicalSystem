using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineType
{
    public class CreateVaccinationTypeRequestValidator : AbstractValidator<CreateVaccinationTypeRequest>
    {
        public CreateVaccinationTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên loại vaccine là bắt buộc.")
                .MaximumLength(100)
                .WithMessage("Tên loại vaccine không được vượt quá 100 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Mô tả không được vượt quá 500 ký tự.");

            RuleFor(x => x.RecommendedAge)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Tuổi khuyến nghị phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.DoseCount)
                .GreaterThan(0)
                .WithMessage("Số liều phải lớn hơn 0.")
                .LessThanOrEqualTo(10)
                .WithMessage("Số liều không được vượt quá 10.");
        }
    }
}

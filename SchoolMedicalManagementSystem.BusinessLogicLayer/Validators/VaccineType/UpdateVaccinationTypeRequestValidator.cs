using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.Vaccine
{
    public class UpdateVaccinationTypeRequestValidator : AbstractValidator<UpdateVaccinationTypeRequest>
    {
        public UpdateVaccinationTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .When(x => x.Name != null)
                .WithMessage("Tên loại vaccine là bắt buộc.")
                .MaximumLength(100)
                .When(x => x.Name != null)
                .WithMessage("Tên loại vaccine không được vượt quá 100 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => x.Description != null)
                .WithMessage("Mô tả không được vượt quá 500 ký tự.");

            RuleFor(x => x.RecommendedAge)
                .GreaterThanOrEqualTo(0)
                .When(x => x.RecommendedAge.HasValue)
                .WithMessage("Tuổi khuyến nghị phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.DoseCount)
                .GreaterThan(0)
                .When(x => x.DoseCount.HasValue)
                .WithMessage("Số liều phải lớn hơn 0.")
                .LessThanOrEqualTo(10)
                .When(x => x.DoseCount.HasValue)
                .WithMessage("Số liều không được vượt quá 10.");
        }
    }
}
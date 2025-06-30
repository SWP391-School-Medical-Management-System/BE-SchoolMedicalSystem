using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators
{
    public class CreateWholeVaccinationSessionRequestValidator : AbstractValidator<CreateWholeVaccinationSessionRequest>
    {
        public CreateWholeVaccinationSessionRequestValidator()
        {
            RuleFor(x => x.VaccineTypeId)
                .NotEmpty()
                .WithMessage("VaccineTypeId là bắt buộc.");

            RuleFor(x => x.SessionName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("SessionName là bắt buộc và tối đa 100 ký tự.");

            RuleFor(x => x.ResponsibleOrganizationName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("ResponsibleOrganizationName là bắt buộc và tối đa 100 ký tự.");

            RuleFor(x => x.Location)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Location là bắt buộc và tối đa 200 ký tự.");

            RuleFor(x => x.StartTime)
                .NotEmpty()
                .GreaterThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("StartTime phải lớn hơn hoặc bằng thời gian hiện tại.");

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .GreaterThan(x => x.StartTime)
                .WithMessage("EndTime phải lớn hơn StartTime.");

            RuleFor(x => x.SideEffect)
                .MaximumLength(500)
                .WithMessage("SideEffect tối đa 500 ký tự.");

            RuleFor(x => x.Contraindication)
                .MaximumLength(500)
                .WithMessage("Contraindication tối đa 500 ký tự.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes tối đa 1000 ký tự.");

            RuleFor(x => x.ClassIds)
                .NotEmpty()
                .WithMessage("Danh sách ClassIds là bắt buộc.");
        }
    }
}
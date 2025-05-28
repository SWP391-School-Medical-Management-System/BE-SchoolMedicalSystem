using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class CreateParentRequestValidator : BaseUserRequestValidator<CreateParentRequest>
{
    public CreateParentRequestValidator()
    {
        RuleFor(x => x.Relationship)
            .NotEmpty().WithMessage("Mối quan hệ là bắt buộc")
            .Must(r => r == "Father" || r == "Mother" || r == "Guardian")
            .WithMessage("Mối quan hệ phải là 'Father', 'Mother', hoặc 'Guardian'");
    }
}
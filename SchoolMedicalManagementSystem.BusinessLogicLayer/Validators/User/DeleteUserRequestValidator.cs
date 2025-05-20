using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.User;

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID người dùng không được để trống");
    }
}
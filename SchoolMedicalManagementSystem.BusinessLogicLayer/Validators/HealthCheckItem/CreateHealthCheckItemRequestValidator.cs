using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckItemRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthCheckItem
{
    public class CreateHealthCheckItemRequestValidator : AbstractValidator<CreateHealthCheckItemRequest>
    {
        public CreateHealthCheckItemRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên hạng mục kiểm tra là bắt buộc.")
                .MaximumLength(100)
                .WithMessage("Tên hạng mục kiểm tra không được vượt quá 100 ký tự.");
          

            RuleFor(x => x.Categories)
                .IsInEnum()
                .WithMessage("Loại hạng mục kiểm tra không hợp lệ.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Mô tả không được vượt quá 500 ký tự.");

            RuleFor(x => x.Unit)
                .NotEmpty()
                .WithMessage("Đơn vị đo là bắt buộc.")
                .MaximumLength(20)
                .WithMessage("Đơn vị đo không được vượt quá 20 ký tự.");

            RuleFor(x => x.MinValue)
                .LessThanOrEqualTo(x => x.MaxValue)
                .When(x => x.MinValue.HasValue && x.MaxValue.HasValue)
                .WithMessage("Giá trị tối thiểu phải nhỏ hơn hoặc bằng giá trị tối đa.");
        }
    }
}

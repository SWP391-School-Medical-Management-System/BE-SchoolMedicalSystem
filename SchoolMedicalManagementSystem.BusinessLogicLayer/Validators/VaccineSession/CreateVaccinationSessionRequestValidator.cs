using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession
{
    public class CreateVaccinationSessionRequestValidator : AbstractValidator<CreateVaccinationSessionRequest>
    {
        public CreateVaccinationSessionRequestValidator()
        {
            RuleFor(x => x.VaccineTypeId)
                .NotEmpty()
                .WithMessage("ID loại vaccine là bắt buộc.");

            RuleFor(x => x.SessionName)
                .NotEmpty()
                .WithMessage("Tên buổi tiêm là bắt buộc.")
                .MaximumLength(100)
                .WithMessage("Tên buổi tiêm không được vượt quá 100 ký tự.");

            RuleFor(x => x.Location)
                .NotEmpty()
                .WithMessage("Địa điểm tiêm là bắt buộc.")
                .MaximumLength(200)
                .WithMessage("Địa điểm tiêm không được vượt quá 200 ký tự.");

            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("Thời gian bắt đầu là bắt buộc.")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Thời gian bắt đầu không được là tương lai.");

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .WithMessage("Thời gian kết thúc là bắt buộc.")
                .GreaterThan(x => x.StartTime)
                .WithMessage("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.")
                .LessThanOrEqualTo(DateTime.UtcNow.AddMonths(6))
                .WithMessage("Thời gian kết thúc không được vượt quá 6 tháng từ hiện tại.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Ghi chú không được vượt quá 500 ký tự.");

            RuleFor(x => x.ClassIds)
                .NotEmpty()
                .WithMessage("Danh sách lớp học là bắt buộc.")
                .Must(ids => ids != null && ids.Any())
                .WithMessage("Danh sách lớp học phải chứa ít nhất một lớp.");
        }
    }
}

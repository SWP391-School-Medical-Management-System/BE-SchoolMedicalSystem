using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.HealthCheck
{
    public class UpdateHealthCheckRequestValidator : AbstractValidator<UpdateHealthCheckRequest>
    {
        public UpdateHealthCheckRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề là bắt buộc")
                .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự");

            RuleFor(x => x.ResponsibleOrganizationName)
                .NotEmpty().WithMessage("Tên tổ chức chịu trách nhiệm là bắt buộc")
                .MaximumLength(200).WithMessage("Tên tổ chức không được vượt quá 200 ký tự");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Địa điểm là bắt buộc")
                .MaximumLength(200).WithMessage("Địa điểm không được vượt quá 200 ký tự");

            RuleFor(x => x.ScheduledDate)
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Ngày lên lịch phải từ hôm nay trở đi");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Thời gian bắt đầu là bắt buộc")
                .LessThanOrEqualTo(x => x.EndTime).WithMessage("Thời gian bắt đầu phải trước hoặc bằng thời gian kết thúc");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("Thời gian kết thúc là bắt buộc");

            RuleFor(x => x.ClassIds)
                .NotEmpty().WithMessage("Danh sách lớp học là bắt buộc")
                .Must(x => x != null && x.Any()).WithMessage("Phải chọn ít nhất một lớp học");

            RuleFor(x => x.HealthCheckItemIds)
                .NotEmpty().WithMessage("Danh sách hạng mục kiểm tra là bắt buộc")
                .Must(x => x != null && x.Any()).WithMessage("Phải chọn ít nhất một hạng mục kiểm tra");
        }
    }
}

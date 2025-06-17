using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.BlogPost
{
    public class CreateBlogPostRequestValidator : AbstractValidator<CreateBlogPostRequest>
    {
        public CreateBlogPostRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề là bắt buộc.")
                .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.");
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung là bắt buộc.");
            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("URL hình ảnh là bắt buộc.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("URL hình ảnh không hợp lệ.");
            RuleFor(x => x.AuthorId)
                .NotEmpty().WithMessage("ID tác giả là bắt buộc.");
            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Tên danh mục là bắt buộc.")
                .Must(category => new[] { "Sức khỏe học đường", "Dinh dưỡng", "Phòng bệnh" }.Contains(category))
                .WithMessage("Danh mục phải là: Sức khỏe học đường, Dinh dưỡng, hoặc Phòng bệnh.");

        }
    }
}

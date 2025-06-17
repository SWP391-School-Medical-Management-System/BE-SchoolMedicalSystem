using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest;
using System;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.BlogPost
{
    public class UpdateBlogPostRequestValidator : AbstractValidator<UpdateBlogPostRequest>
    {
        public UpdateBlogPostRequestValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Title));

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung là bắt buộc.")
                .When(x => !string.IsNullOrEmpty(x.Content)); 

            RuleFor(x => x.ImageUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("URL hình ảnh không hợp lệ.")
                .When(x => !string.IsNullOrEmpty(x.ImageUrl));

            RuleFor(x => x.CategoryName)
                .Must(category => string.IsNullOrEmpty(category) || new[] { "Sức khỏe học đường", "Dinh dưỡng", "Phòng bệnh" }.Contains(category))
                .WithMessage("Danh mục phải là: Sức khỏe học đường, Dinh dưỡng, hoặc Phòng bệnh.")
                .When(x => !string.IsNullOrEmpty(x.CategoryName));

            RuleFor(x => x)
                .Must(HaveAtLeastOneField).WithMessage("Phải cung cấp ít nhất một trường để cập nhật.");
        }

        private bool HaveAtLeastOneField(UpdateBlogPostRequest request)
        {
            return !string.IsNullOrEmpty(request.Title) || !string.IsNullOrEmpty(request.Content) ||
                   !string.IsNullOrEmpty(request.ImageUrl) || !string.IsNullOrEmpty(request.CategoryName) ||
                   request.IsPublished.HasValue;
        }
    }
}
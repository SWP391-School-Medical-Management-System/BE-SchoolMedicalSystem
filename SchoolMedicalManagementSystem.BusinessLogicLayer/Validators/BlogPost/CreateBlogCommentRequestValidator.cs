using FluentValidation;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.BlogPost
{
    public class CreateBlogCommentRequestValidator : AbstractValidator<CreateBlogCommentRequest>
    {
        public CreateBlogCommentRequestValidator()
        {
            RuleFor(x => x.PostId)
                .NotEmpty().WithMessage("ID bài viết là bắt buộc.");
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("ID người dùng là bắt buộc.");
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung bình luận là bắt buộc.")
                .MaximumLength(1000).WithMessage("Nội dung không được vượt quá 1000 ký tự.");
        }
    }
}

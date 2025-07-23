using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BlogPostResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IBlogPostService
    {
        Task<BaseListResponse<BlogPostResponse>> GetBlogPostsAsync(
            int pageIndex = 1, int pageSize = 10,
            string searchTerm = "",
            string category = null,
            bool? isPublished = null,
            string orderBy = null,
            CancellationToken cancellationToken = default);
        Task<BaseResponse<BlogPostResponse>> GetBlogPostByIdAsync(
            Guid id);
        Task<BaseResponse<BlogPostResponse>> CreateBlogPostAsync(
            CreateBlogPostRequest model);
        Task<BaseResponse<BlogPostResponse>> UpdateBlogPostAsync(
            Guid id, UpdateBlogPostRequest model);
        Task<BaseResponse<bool>> DeleteBlogPostAsync(Guid id);
        Task<BaseListResponse<BlogCommentResponse>> GetBlogCommentsByBlogIdAsync(
            Guid blogId,
            int pageIndex = 1,
            int pageSize = 10,
            bool? isApproved = null,
            string orderBy = null,
            CancellationToken cancellationToken = default);
        Task<BaseResponse<BlogCommentResponse>> CreateBlogCommentAsync(
            CreateBlogCommentRequest model);
        Task<BaseResponse<bool>> ApproveBlogCommentAsync(
            Guid commentId);
        Task<BaseResponse<bool>> DeleteBlogCommentAsync(
            Guid commentId);
        Task<BaseListResponse<BlogPostResponse>> GetFeaturedBlogPostsAsync(
            int pageIndex = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);
    }
}

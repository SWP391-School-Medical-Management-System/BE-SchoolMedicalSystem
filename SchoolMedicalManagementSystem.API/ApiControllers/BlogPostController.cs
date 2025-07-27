using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BlogPostResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/blog-posts")]
public class BlogPostController : ControllerBase
{
    private readonly IBlogPostService _blogPostService;

    public BlogPostController(IBlogPostService blogPostService)
    {
        _blogPostService = blogPostService;
    }

    #region Blog Post Management

    [HttpGet]
    public async Task<ActionResult<BaseListResponse<BlogPostResponse>>> GetBlogPosts(
      [FromQuery] int pageIndex = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string searchTerm = "",
      [FromQuery] string category = null,
      [FromQuery] bool? isPublished = null,
      [FromQuery] string orderBy = null,
      CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(BaseListResponse<BlogPostResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));
            var response = await _blogPostService.GetBlogPostsAsync(pageIndex, pageSize, searchTerm, category, isPublished, orderBy, cancellationToken);
            return response.Success ? Ok(response) : NotFound(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseListResponse<BlogPostResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseResponse<BlogPostResponse>>> GetBlogPostById(Guid id)
    {
        try
        {
            var response = await _blogPostService.GetBlogPostByIdAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<BlogPostResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<BlogPostResponse>>> CreateBlogPost([FromBody] CreateBlogPostRequest model)
    {
        try
        {
            var response = await _blogPostService.CreateBlogPostAsync(model);
            return response.Success ? CreatedAtAction(nameof(GetBlogPostById), new { id = response.Data.Id }, response) : BadRequest(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<BlogPostResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<BlogPostResponse>>> UpdateBlogPost(
        Guid id, 
        [FromBody] UpdateBlogPostRequest model)
    {
        try
        {
            var response = await _blogPostService.UpdateBlogPostAsync(id, model);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<BlogPostResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteBlogPost(Guid id)
    {
        try
        {
            var response = await _blogPostService.DeleteBlogPostAsync(id);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("featured")]
    public async Task<ActionResult<BaseListResponse<BlogPostResponse>>> GetFeaturedBlogPosts(
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(BaseListResponse<BlogPostResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));
            var response = await _blogPostService.GetFeaturedBlogPostsAsync(pageIndex, pageSize, cancellationToken);
            return response.Success ? Ok(response) : NotFound(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseListResponse<BlogPostResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }
    #endregion

    #region Blog Comment Management

    [HttpGet("{blogId}/comments")]
    public async Task<ActionResult<BaseListResponse<BlogCommentResponse>>> GetBlogCommentsByBlogId(
        Guid blogId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isApproved = null,
        [FromQuery] string orderBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(BaseListResponse<BlogCommentResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));
            var response = await _blogPostService.GetBlogCommentsByBlogIdAsync(blogId, pageIndex, pageSize, isApproved, orderBy, cancellationToken);
            return response.Success ? Ok(response) : NotFound(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseListResponse<BlogCommentResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("comments")]
    public async Task<ActionResult<BaseResponse<BlogCommentResponse>>> CreateBlogComment([FromBody] CreateBlogCommentRequest model)
    {
        try
        {
            var response = await _blogPostService.CreateBlogCommentAsync(model);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<BlogCommentResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("comments/{commentId}/approve")]
    [Authorize(Roles = "MANAGER, SCHOOLNURSE")] 
    public async Task<ActionResult<BaseResponse<bool>>> ApproveBlogComment(Guid commentId)
    {
        try
        {
            var response = await _blogPostService.ApproveBlogCommentAsync(commentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("comments/{commentId}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteBlogComment(Guid commentId)
    {
        try
        {
            var response = await _blogPostService.DeleteBlogCommentAsync(commentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion
}
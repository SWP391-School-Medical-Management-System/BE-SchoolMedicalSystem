using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.BlogPostRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BlogPostResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class BlogPostService : IBlogPostService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BlogPostService> _logger;
    private readonly IValidator<CreateBlogPostRequest> _createBlogPostValidator;
    private readonly IValidator<UpdateBlogPostRequest> _updateBlogPostValidator;
    private readonly IValidator<CreateBlogCommentRequest> _createBlogCommentValidator;

    private const string BLOG_POST_CACHE_PREFIX = "blog_post";
    private const string BLOG_POST_LIST_PREFIX = "blog_posts_list";

    public BlogPostService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<BlogPostService> logger, ICacheService cacheService,
        IValidator<CreateBlogPostRequest> createBlogPostValidator,
        IValidator<UpdateBlogPostRequest> updateBlogPostValidator,
        IValidator<CreateBlogCommentRequest> createBlogCommentValidator)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _createBlogPostValidator = createBlogPostValidator;
        _updateBlogPostValidator = updateBlogPostValidator;
        _createBlogCommentValidator = createBlogCommentValidator;
    }
    #region Blog Post
    public async Task<BaseListResponse<BlogPostResponse>> GetBlogPostsAsync(int pageIndex = 1, int pageSize = 10, string searchTerm = "", string category = null, bool? isPublished = null, string orderBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"blog_list_{pageIndex}_{pageSize}_{searchTerm}_{category ?? ""}_{isPublished ?? false}_{orderBy ?? ""}";
            var cachedResult = await _cacheService.GetAsync<BaseListResponse<BlogPostResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Trả về kết quả từ bộ nhớ đệm cho key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .Include(bp => bp.Author)
                .Include(bp => bp.Comments.Where(c => !c.IsDeleted && c.IsApproved))
                .Where(bp => !bp.IsDeleted);

            _logger.LogInformation("Số lượng bài viết trước khi lọc: {Count}", await query.CountAsync(cancellationToken));

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(bp => bp.Title.Contains(searchTerm) || bp.Content.Contains(searchTerm));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(bp => bp.CategoryName == category);
            if (isPublished.HasValue)
                query = query.Where(bp => bp.IsPublished == isPublished.Value);

            _logger.LogInformation("Số lượng bài viết sau khi lọc: {Count}", await query.CountAsync(cancellationToken));

            query = ApplyBlogPostOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var blogPosts = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Số lượng bài viết trả về: {Count}", blogPosts.Count);

            var responses = _mapper.Map<List<BlogPostResponse>>(blogPosts);

            var result = BaseListResponse<BlogPostResponse>.SuccessResult(responses, totalCount, pageSize, pageIndex, "Lấy danh sách bài viết thành công.");
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi lấy danh sách bài viết");
            return BaseListResponse<BlogPostResponse>.ErrorResult("Lỗi lấy danh sách bài viết.");
        }
    }
    public async Task<BaseResponse<BlogPostResponse>> GetBlogPostByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"blog_{id}";
            var cachedResult = await _cacheService.GetAsync<BaseResponse<BlogPostResponse>>(cacheKey);
            if (cachedResult != null) return cachedResult;

            var blogPost = await _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .Include(bp => bp.Author)
                .Include(bp => bp.Comments.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(bp => bp.Id == id && !bp.IsDeleted);

            if (blogPost == null)
                return BaseResponse<BlogPostResponse>.ErrorResult("Không tìm thấy bài viết.");

            var response = BaseResponse<BlogPostResponse>.SuccessResult(_mapper.Map<BlogPostResponse>(blogPost), "Lấy bài viết thành công.");
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi lấy bài viết theo ID: {Id}", id);
            return BaseResponse<BlogPostResponse>.ErrorResult("Lỗi lấy bài viết.");
        }
    }

    public async Task<BaseResponse<BlogPostResponse>> CreateBlogPostAsync(CreateBlogPostRequest model)
    {
        try
        {
            var validationResult = await _createBlogPostValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
                return BaseResponse<BlogPostResponse>.ErrorResult(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var author = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == model.AuthorId && !u.IsDeleted && u.IsActive);
            if (author == null)
            {
                _logger.LogWarning("Tác giả không tồn tại: {AuthorId}", model.AuthorId);
                return BaseResponse<BlogPostResponse>.ErrorResult("Tác giả không tồn tại.");
            }
            if (!author.UserRoles.Any(ur => ur.Role.Name == "MANAGER"))
            {
                _logger.LogWarning("Tác giả {AuthorId} không phải là MANAGER", model.AuthorId);
                return BaseResponse<BlogPostResponse>.ErrorResult("Tác giả không phải là quản lý.");
            }

            var blogPost = _mapper.Map<BlogPost>(model);
            blogPost.Id = Guid.NewGuid();
            blogPost.CreatedDate = DateTime.Now;
            blogPost.IsFeatured = model.IsFeatured ?? false;

            await _unitOfWork.GetRepositoryByEntity<BlogPost>().AddAsync(blogPost);
            await _unitOfWork.SaveChangesAsync();

            await InvalidateCacheAsync();
            return BaseResponse<BlogPostResponse>.SuccessResult(_mapper.Map<BlogPostResponse>(blogPost), "Tạo bài viết thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo bài viết với dữ liệu: {@Model}", model);
            return BaseResponse<BlogPostResponse>.ErrorResult($"Lỗi tạo bài viết: {ex.Message}");
        }
    }
    public async Task<BaseResponse<BlogPostResponse>> UpdateBlogPostAsync(Guid id, UpdateBlogPostRequest model)
    {
        try
        {
            var validationResult = await _updateBlogPostValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
                return BaseResponse<BlogPostResponse>.ErrorResult(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var blogPost = await _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .FirstOrDefaultAsync(bp => bp.Id == id && !bp.IsDeleted);
            if (blogPost == null)
                return BaseResponse<BlogPostResponse>.ErrorResult("Không tìm thấy bài viết.");

            _mapper.Map(model, blogPost);
            blogPost.LastUpdatedDate = DateTime.Now;
            blogPost.IsFeatured = model.IsFeatured ?? blogPost.IsFeatured;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateCacheAsync();
            return BaseResponse<BlogPostResponse>.SuccessResult(_mapper.Map<BlogPostResponse>(blogPost), "Cập nhật bài viết thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi cập nhật bài viết");
            return BaseResponse<BlogPostResponse>.ErrorResult("Lỗi cập nhật bài viết.");
        }
    }

    public async Task<BaseResponse<bool>> DeleteBlogPostAsync(Guid id)
    {
        try
        {
            var blogPost = await _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .FirstOrDefaultAsync(bp => bp.Id == id && !bp.IsDeleted);
            if (blogPost == null)
                return BaseResponse<bool>.ErrorResult("Không tìm thấy bài viết.");

            blogPost.IsDeleted = true;
            blogPost.LastUpdatedDate = DateTime.Now;
            _logger.LogInformation("Đánh dấu bài viết với ID {Id} là đã xóa: IsDeleted = {IsDeleted}", id, blogPost.IsDeleted);

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Lưu thay đổi vào cơ sở dữ liệu cho bài viết ID {Id}", id);

            await InvalidateCacheAsync();
            _logger.LogInformation("Bộ nhớ đệm đã được xóa sau khi xóa bài viết ID {Id}", id);

            return BaseResponse<bool>.SuccessResult(true, "Xóa bài viết thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xóa bài viết với ID: {Id}", id);
            return BaseResponse<bool>.ErrorResult("Lỗi xóa bài viết.");
        }
    }

    public async Task<BaseListResponse<BlogPostResponse>> GetFeaturedBlogPostsAsync(int pageIndex = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"featured_blog_list_{pageIndex}_{pageSize}";
            var cachedResult = await _cacheService.GetAsync<BaseListResponse<BlogPostResponse>>(cacheKey);
            if (cachedResult != null) return cachedResult;

            var query = _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .Include(bp => bp.Author)
                .Include(bp => bp.Comments.Where(c => !c.IsDeleted && c.IsApproved))
                .Where(bp => !bp.IsDeleted && bp.IsFeatured && bp.IsPublished);

            var totalCount = await query.CountAsync(cancellationToken);
            var blogPosts = await query
                .OrderByDescending(bp => bp.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<BlogPostResponse>>(blogPosts);

            var result = BaseListResponse<BlogPostResponse>.SuccessResult(responses, totalCount, pageSize, pageIndex, "Lấy danh sách bài viết nổi bật thành công.");
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi lấy danh sách bài viết nổi bật");
            return BaseListResponse<BlogPostResponse>.ErrorResult("Lỗi lấy danh sách bài viết nổi bật.");
        }
    }

    #endregion
    #region Blog Comment

    public async Task<BaseResponse<BlogCommentResponse>> CreateBlogCommentAsync(CreateBlogCommentRequest model)
    {
        try
        {
            if (model == null)
            {
                _logger.LogError("CreateBlogCommentRequest model is null");
                return BaseResponse<BlogCommentResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ.");
            }

            if (_createBlogCommentValidator == null)
            {
                _logger.LogError("CreateBlogCommentValidator is null");
                return BaseResponse<BlogCommentResponse>.ErrorResult("Validator không được khởi tạo.");
            }

            var validationResult = await _createBlogCommentValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
                return BaseResponse<BlogCommentResponse>.ErrorResult(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var blogPostRepository = _unitOfWork.GetRepositoryByEntity<BlogPost>();
            if (blogPostRepository == null)
            {
                _logger.LogError("BlogPost repository is null");
                return BaseResponse<BlogCommentResponse>.ErrorResult("Repository không khả dụng.");
            }

            var post = await blogPostRepository.GetQueryable()
                .Include(bp => bp.Comments)
                .FirstOrDefaultAsync(bp => bp.Id == model.PostId && !bp.IsDeleted);
            if (post == null)
                return BaseResponse<BlogCommentResponse>.ErrorResult("Bài viết không tồn tại.");

            var userRepository = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            if (userRepository == null)
            {
                _logger.LogError("ApplicationUser repository is null");
                return BaseResponse<BlogCommentResponse>.ErrorResult("User repository không khả dụng.");
            }

            var user = await userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == model.UserId && !u.IsDeleted && u.IsActive);
            if (user == null)
                return BaseResponse<BlogCommentResponse>.ErrorResult("Người dùng không tồn tại.");

            var comment = new BlogComment
            {
                Id = Guid.NewGuid(),
                PostId = model.PostId,
                UserId = model.UserId,
                Content = model.Content ?? string.Empty,
                IsApproved = false,
                CreatedDate = DateTime.Now,
                IsDeleted = false
            };

            var commentRepository = _unitOfWork.GetRepositoryByEntity<BlogComment>();
            if (commentRepository == null)
            {
                _logger.LogError("BlogComment repository is null");
                return BaseResponse<BlogCommentResponse>.ErrorResult("Comment repository không khả dụng.");
            }

            await commentRepository.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Comment sau khi lưu: {@Comment}", comment);

            var savedComment = await commentRepository.GetQueryable()
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            if (savedComment == null)
            {
                _logger.LogError("Không thể tải lại comment với ID: {CommentId}", comment.Id);
                return BaseResponse<BlogCommentResponse>.ErrorResult("Không thể tải lại bình luận sau khi tạo.");
            }

            if (_mapper == null)
            {
                _logger.LogError("AutoMapper is null");
                return BaseResponse<BlogCommentResponse>.ErrorResult("Mapper không khả dụng.");
            }

            var response = _mapper.Map<BlogCommentResponse>(savedComment);
            if (response == null)
            {
                _logger.LogError("Mapper trả về null cho comment: {CommentId}", savedComment.Id);
                return BaseResponse<BlogCommentResponse>.ErrorResult("Lỗi mapping dữ liệu.");
            }

            await InvalidateCacheAsync();
            return BaseResponse<BlogCommentResponse>.SuccessResult(response, "Tạo bình luận thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo bình luận với model: {@Model}", model);
            return BaseResponse<BlogCommentResponse>.ErrorResult($"Lỗi tạo bình luận: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> ApproveBlogCommentAsync(Guid commentId)
    {
        try
        {
            var comment = await _unitOfWork.GetRepositoryByEntity<BlogComment>().GetQueryable()
                .Include(c => c.Post)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                return BaseResponse<bool>.ErrorResult("Bình luận không tồn tại.");

            comment.IsApproved = true;
            comment.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateCacheAsync();
            return BaseResponse<bool>.SuccessResult(true, "Phê duyệt bình luận thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi phê duyệt bình luận");
            return BaseResponse<bool>.ErrorResult("Lỗi phê duyệt bình luận.");
        }
    }

    public async Task<BaseResponse<bool>> DeleteBlogCommentAsync(Guid commentId)
    {
        try
        {
            var comment = await _unitOfWork.GetRepositoryByEntity<BlogComment>().GetQueryable()
                .Include(c => c.Post)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                return BaseResponse<bool>.ErrorResult("Bình luận không tồn tại.");

            comment.IsDeleted = true;
            comment.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateCacheAsync();
            return BaseResponse<bool>.SuccessResult(true, "Xóa bình luận thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xóa bình luận");
            return BaseResponse<bool>.ErrorResult("Lỗi xóa bình luận.");
        }
    }

    #endregion

    #region Helper Methods
    private IQueryable<BlogPost> ApplyBlogPostOrdering(IQueryable<BlogPost> query, string orderBy)
    {
        if (string.IsNullOrEmpty(orderBy))
            return query.OrderByDescending(bp => bp.CreatedDate); 

        switch (orderBy.ToLower())
        {
            case "title":
                return query.OrderBy(bp => bp.Title);
            case "createddate_desc":
                return query.OrderByDescending(bp => bp.CreatedDate);
            case "createddate_asc":
                return query.OrderBy(bp => bp.CreatedDate);
            default:
                return query.OrderByDescending(bp => bp.CreatedDate);
        }
    }
    private async Task InvalidateCacheAsync()
    {
        try
        {
            await _cacheService.RemoveAsync("blog_list_*");
            _logger.LogInformation("Đã xóa các key bắt đầu bằng 'blog_list_'");
            await _cacheService.RemoveByPrefixAsync(BLOG_POST_CACHE_PREFIX);
            _logger.LogInformation("Đã xóa các key theo prefix '{BLOG_POST_CACHE_PREFIX}'", BLOG_POST_CACHE_PREFIX);
            await _cacheService.RemoveByPrefixAsync(BLOG_POST_LIST_PREFIX);
            _logger.LogInformation("Đã xóa các key theo prefix '{BLOG_POST_LIST_PREFIX}'", BLOG_POST_LIST_PREFIX);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa bộ nhớ đệm");
            throw; 
        }
    }
    #endregion
}
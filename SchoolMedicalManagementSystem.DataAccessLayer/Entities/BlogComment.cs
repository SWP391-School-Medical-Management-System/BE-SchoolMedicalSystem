namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Bình luận trên bài viết blog
/// </summary>
public class BlogComment : BaseEntity
{
    public Guid PostId { get; set; }      // ID bài viết
    public Guid? UserId { get; set; }      // ID người bình luận
    public string Content { get; set; }   // Nội dung bình luận
    public bool IsApproved { get; set; } 
    
    public virtual BlogPost Post { get; set; } // Bài viết được bình luận
    public virtual ApplicationUser User { get; set; } // Người bình luận
}
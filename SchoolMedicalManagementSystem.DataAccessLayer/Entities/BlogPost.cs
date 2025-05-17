namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Bài viết blog chia sẻ thông tin y tế, kinh nghiệm
/// </summary>
public class BlogPost : BaseEntity
{
    public string Title { get; set; }         // Tiêu đề bài viết
    public string Content { get; set; }       // Nội dung bài viết
    public string ImageUrl { get; set; }      // URL hình ảnh minh họa
    public Guid AuthorId { get; set; }        // ID tác giả
    public bool IsPublished { get; set; }     // Đã xuất bản chưa
    public string CategoryName { get; set; }  // Tên danh mục, ví dụ: "Sức khỏe học đường", "Dinh dưỡng", "Phòng bệnh"
    
    public virtual ApplicationUser Author { get; set; }     // Tác giả bài viết
    public virtual ICollection<BlogComment> Comments { get; set; } // Bình luận
}

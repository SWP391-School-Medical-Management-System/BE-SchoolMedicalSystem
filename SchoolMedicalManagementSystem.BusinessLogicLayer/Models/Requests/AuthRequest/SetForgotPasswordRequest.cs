using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

public class SetForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải có 6 chữ số.")]
    public string OTP { get; set; }

    [Required]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    public string NewPassword { get; set; }

    [Required]
    [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

public class VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải có 6 chữ số.")]
    public string Otp { get; set; }
}
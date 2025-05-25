using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
        
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; }
        
    [Required]
    [MinLength(6)]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; }
}
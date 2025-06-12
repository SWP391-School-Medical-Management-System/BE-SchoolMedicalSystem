using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
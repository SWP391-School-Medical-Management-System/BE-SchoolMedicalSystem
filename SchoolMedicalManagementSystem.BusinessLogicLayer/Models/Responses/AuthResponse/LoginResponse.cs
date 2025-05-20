namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.AuthResponse;

public class LoginResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
}
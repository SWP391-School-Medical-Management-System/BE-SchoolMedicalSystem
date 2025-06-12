namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;

public class RefreshTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
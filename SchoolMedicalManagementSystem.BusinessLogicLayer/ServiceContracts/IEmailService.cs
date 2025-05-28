namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IEmailService
{
    Task SendAccountCreationEmailAsync(string to, string username, string password);
    Task SendForgotPasswordOtpAsync(string to, string otp, int expiryMinutes);
    Task SendPasswordResetConfirmationAsync(string to, string fullName, string ipAddress = "Unknown");
    Task SendPasswordChangedEmailAsync(string to, string username);
}
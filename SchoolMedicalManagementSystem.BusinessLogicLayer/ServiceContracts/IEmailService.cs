namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IEmailService
{
    Task SendAccountCreationEmailAsync(string to, string username, string password);
    Task SendForgotPasswordOtpAsync(string to, string otp, int expiryMinutes);
    Task SendPasswordResetConfirmationAsync(string to, string fullName, string ipAddress = "Unknown");
    Task SendChildAddedNotificationAsync(string parentEmail, string childName, string parentName,
        string studentCode = "", string className = "", string relationship = "", int totalChildren = 1,
        string parentPhone = "");
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
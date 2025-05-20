namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IEmailService
{
    Task SendAccountCreationEmailAsync(string to, string username, string password);
    Task SendPasswordResetLinkAsync(string to, string username, string resetUrl);
}
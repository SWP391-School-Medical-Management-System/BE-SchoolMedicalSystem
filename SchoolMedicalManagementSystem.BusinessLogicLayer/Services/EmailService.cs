using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services.EmailService;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private async Task SendEmailInternalAsync(string to, string subject, string htmlBody)
    {
        var smtpClient = new SmtpClient
        {
            Host = _configuration["SMTP:Host"],
            Port = int.Parse(_configuration["SMTP:Port"]),
            Credentials = new NetworkCredential(
                _configuration["SMTP:Username"],
                _configuration["SMTP:Password"]
            ),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_configuration["SMTP:Username"]),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(to);

        await smtpClient.SendMailAsync(mailMessage);
    }

    public async Task SendAccountCreationEmailAsync(string to, string username, string password)
    {
        var template = await GetEmailTemplateAsync("CreateUserAccountEmailTemplate.html");

        var systemUrl = _configuration["System:Url"] ?? "https://schoolmedical.example.com";
        var supportEmail = _configuration["System:SupportEmail"] ?? "support@schoolmedical.example.com";

        var emailBody = template
            .Replace("{USERNAME}", username)
            .Replace("{PASSWORD}", password)
            .Replace("{URL}", systemUrl)
            .Replace("{SUPPORT_EMAIL}", supportEmail)
            .Replace("{CURRENT_TIME}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
            .Replace("{YEAR}", DateTime.Now.Year.ToString());

        await SendEmailInternalAsync(to, "Thông Tin Tài Khoản School Medical Management System", emailBody);
    }


    public async Task SendForgotPasswordOtpAsync(string to, string otp, int expiryMinutes)
    {
        var template = await GetEmailTemplateAsync("ForgotPasswordOtpEmailTemplate.html");

        var systemUrl = _configuration["System:Url"] ?? "https://schoolmedical.example.com";
        var supportEmail = _configuration["System:SupportEmail"] ?? "support@schoolmedical.example.com";

        var username = to.Split('@')[0];

        var emailBody = template
            .Replace("{USERNAME}", username)
            .Replace("{EMAIL}", to)
            .Replace("{OTP_CODE}", otp)
            .Replace("{EXPIRY_MINUTES}", expiryMinutes.ToString())
            .Replace("{URL}", systemUrl)
            .Replace("{SUPPORT_EMAIL}", supportEmail)
            .Replace("{YEAR}", DateTime.Now.Year.ToString());

        await SendEmailInternalAsync(to, "Mã OTP Đặt Lại Mật Khẩu - School Medical Management System", emailBody);
    }

    public async Task SendPasswordResetConfirmationAsync(string to, string fullName, string ipAddress = "Unknown")
    {
        var template = await GetEmailTemplateAsync("PasswordResetConfirmationEmailTemplate.html");

        var systemUrl = _configuration["System:Url"] ?? "https://schoolmedical.example.com";
        var supportEmail = _configuration["System:SupportEmail"] ?? "support@schoolmedical.example.com";
        var loginUrl = $"{systemUrl}/login";

        var emailBody = template
            .Replace("{FULL_NAME}", fullName)
            .Replace("{EMAIL}", to)
            .Replace("{RESET_TIME}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
            .Replace("{IP_ADDRESS}", ipAddress)
            .Replace("{LOGIN_URL}", loginUrl)
            .Replace("{CURRENT_TIME}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
            .Replace("{URL}", systemUrl)
            .Replace("{SUPPORT_EMAIL}", supportEmail)
            .Replace("{YEAR}", DateTime.Now.Year.ToString());

        await SendEmailInternalAsync(to, "Xác Nhận Đặt Lại Mật Khẩu Thành Công - School Medical Management System",
            emailBody);
    }

    private async Task<string> GetEmailTemplateAsync(string templateFileName)
    {
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFileName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Không tìm thấy email template.", templatePath);

        var template = await File.ReadAllTextAsync(templatePath);
        return template;
    }
    public async Task SendPasswordChangedEmailAsync(string to, string username)
    {
        var template = await GetEmailTemplateAsync("PasswordChangedEmailTemplate.html");

        var systemUrl = _configuration["System:Url"] ?? "https://schoolmedical.example.com";
        var supportEmail = _configuration["System:SupportEmail"] ?? "support@schoolmedical.example.com";
        var loginUrl = $"{systemUrl}/login";

        var emailBody = template
            .Replace("{USERNAME}", username)
            .Replace("{EMAIL}", to)
            .Replace("{CHANGE_TIME}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
            .Replace("{LOGIN_URL}", loginUrl)
            .Replace("{URL}", systemUrl)
            .Replace("{SUPPORT_EMAIL}", supportEmail)
            .Replace("{YEAR}", DateTime.Now.Year.ToString());

        await SendEmailInternalAsync(to, "Thông Báo Đổi Mật Khẩu Thành Công - School Medical Management System", emailBody);
    }
}
namespace iNaturalist_Lite.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string username);
    Task SendOtpEmailAsync(string toEmail, string username, string otp);
    Task SendPasswordChangedEmailAsync(string toEmail, string username);
}

namespace iNaturalist_Lite.Services;

public interface IEmailService
{
    Task<string> SendWelcomeEmailAsync(string toEmail, string username);
    Task<string> SendOtpEmailAsync(string toEmail, string username, string otp);
    Task<string> SendPasswordChangedEmailAsync(string toEmail, string username);
}

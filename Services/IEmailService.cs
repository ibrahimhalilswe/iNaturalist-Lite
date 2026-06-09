namespace iNaturalist_Lite.Services;

public interface IEmailService
{
    Task<bool> SendWelcomeEmailAsync(string toEmail, string username);
    Task<bool> SendOtpEmailAsync(string toEmail, string username, string otp);
    Task<bool> SendPasswordChangedEmailAsync(string toEmail, string username);
}

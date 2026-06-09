using System.Net;
using System.Net.Mail;

namespace iNaturalist_Lite.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    private async Task<string> SendMailAsync(string toEmail, string subject, string body)
    {
        var smtpEmail = _config["Smtp:Email"];
        var smtpPass = _config["Smtp:AppPassword"];

        if (string.IsNullOrEmpty(smtpEmail) || string.IsNullOrEmpty(smtpPass))
            return "SMTP ayarları eksik."; // Configured without SMTP, skip gracefully

        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpEmail, smtpPass),
                EnableSsl = true
            };
            var mail = new MailMessage(smtpEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };
            await client.SendMailAsync(mail);
            return string.Empty; // Success
        }
        catch (Exception ex)
        {
            Console.WriteLine("Mail gönderilemedi: " + ex.Message);
            return ex.Message; // Return the exact error message
        }
    }

    public Task<string> SendWelcomeEmailAsync(string toEmail, string username)
    {
        var body = $@"
<div style='font-family: ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #374151; line-height: 1.6; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 12px;'>
    <h2 style='color: #10b981; margin-bottom: 16px;'>Merhaba {username}! 👋</h2>
    <p style='font-size: 16px; margin-bottom: 24px;'>iNaturalist Lite'a katıldığın için teşekkür ederiz. Seni aramızda görmek harika!</p>
    
    <h3 style='color: #111827; font-size: 18px; margin-bottom: 12px;'>İşte iNaturalist Lite ile yapabileceklerinden bazıları:</h3>
    <ul style='padding-left: 20px; margin-bottom: 24px;'>
        <li style='margin-bottom: 8px;'><b>Doğayı Keşfet:</b> Gördüğün bitkilerin fotoğraflarını çekip yapay zeka ile türlerini saniyeler içinde öğrenebilirsin.</li>
        <li style='margin-bottom: 8px;'><b>Dijital Herbaryum Kur:</b> Kendi bitki koleksiyonunu oluşturup profilinde gururla sergileyebilirsin.</li>
        <li style='margin-bottom: 8px;'><b>Toplulukla Etkileşime Geç:</b> Diğer doğa severlerin keşiflerini inceleyebilir, beğenebilir ve yorum yapabilirsin.</li>
    </ul>
    
    <p style='font-size: 16px; margin-bottom: 24px;'>Uygulamamızı hemen şimdi Web veya Mobil üzerinden kullanarak çevrendeki biyoçeşitliliği belgelemeye başlayabilirsin. Doğa seni bekliyor!</p>
    
    <p style='font-size: 16px; font-weight: bold; color: #10b981;'>❤️ iNaturalist Lite Ekibi</p>
</div>";
        return SendMailAsync(toEmail, "iNaturalist Lite'a Hoş Geldin! 🌱", body);
    }

    public Task<string> SendOtpEmailAsync(string toEmail, string username, string otp)
    {
        var body = $@"
<div style='font-family: ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #374151; line-height: 1.6; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 12px;'>
    <h2 style='color: #10b981; margin-bottom: 16px;'>Şifre Sıfırlama Kodu 🔐</h2>
    <p style='font-size: 16px; margin-bottom: 24px;'>Merhaba {username}, iNaturalist Lite hesabın için bir şifre sıfırlama talebi aldık.</p>
    
    <div style='background-color: #f3f4f6; padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 24px;'>
        <p style='font-size: 14px; color: #6b7280; margin-bottom: 8px; margin-top: 0;'>Doğrulama Kodun:</p>
        <h2 style='font-size: 36px; color: #10b981; letter-spacing: 8px; margin: 0;'>{otp}</h2>
    </div>
    
    <p style='font-size: 14px; margin-bottom: 24px; color: #4b5563;'>Bu kod <b>15 dakika</b> boyunca geçerlidir. Eğer şifre sıfırlama talebinde bulunmadıysan, bu e-postayı güvenle görmezden gelebilirsin.</p>
    
    <p style='font-size: 16px; font-weight: bold; color: #10b981;'>❤️ iNaturalist Lite Ekibi</p>
</div>";
        return SendMailAsync(toEmail, "iNaturalist Lite - Şifre Sıfırlama Kodu", body);
    }

    public Task<string> SendPasswordChangedEmailAsync(string toEmail, string username)
    {
        var body = $@"
<div style='font-family: ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #374151; line-height: 1.6; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 12px;'>
    <h2 style='color: #10b981; margin-bottom: 16px;'>Şifren Başarıyla Değiştirildi ✅</h2>
    <p style='font-size: 16px; margin-bottom: 24px;'>Merhaba {username}, iNaturalist Lite hesabının şifresi az önce başarıyla güncellendi.</p>
    
    <p style='font-size: 16px; margin-bottom: 24px;'>Artık yeni şifrenle Web veya Mobil uygulamamıza güvenle giriş yapabilir ve doğayı keşfetmeye kaldığın yerden devam edebilirsin.</p>
    
    <div style='background-color: #fffbeb; border: 1px solid #fde68a; padding: 16px; border-radius: 8px; margin-bottom: 24px;'>
        <p style='font-size: 14px; color: #92400e; margin: 0;'><b>Güvenlik Uyarısı:</b> Eğer bu şifre değişikliğini <u>sen yapmadıysan</u>, hesabının güvenliği için lütfen derhal bizimle iletişime geç.</p>
    </div>
    
    <p style='font-size: 16px; font-weight: bold; color: #10b981;'>❤️ iNaturalist Lite Ekibi</p>
</div>";
        return SendMailAsync(toEmail, "iNaturalist Lite - Şifreniz Değiştirildi", body);
    }
}

using System.Text;
using System.Text.Json;

public class PlantNetValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public PlantNetValidationService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<(bool isValid, string message)> IsPlantImageAsync(Stream imageStream, string mimeType)
    {
        var apiKey = _config["PlantNet:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            return (true, ""); // Anahtar yoksa es geç
        }

        try
        {
            var url = $"https://my-api.plantnet.org/v2/identify/all?api-key={apiKey}&include-related-images=false&no-reject=false&lang=tr";
            
            imageStream.Position = 0;
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            ms.Position = 0;

            using var multipart = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(ms.ToArray());
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType ?? "image/jpeg");
            multipart.Add(imageContent, "images", "photo.jpg");
            multipart.Add(new StringContent("auto"), "organs");

            var response = await _httpClient.PostAsync(url, multipart);
            
            // 404 Not Found in PlantNet means "Species not found" (Not a plant!)
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, "Sistem bu fotoğrafta bir bitki algılayamadı. Lütfen doğadan geçerli bir bitki fotoğrafı yükleyin.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"PlantNet API Error: {response.StatusCode} - {err}");
                // Limit aşımı gibi durumlarda (429) yüklemeye izin ver veya hata fırlat
                if ((int)response.StatusCode == 429) 
                    return (true, ""); 

                return (false, $"Doğrulama servisi hatası ({response.StatusCode})."); 
            }

            // 200 OK means it found a plant!
            return (true, "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PlantNet Validation Exception: {ex.Message}");
            return (true, ""); // Hata durumunda varsayılan olarak izin ver
        }
    }
}

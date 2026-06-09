namespace iNaturalist_Lite.Services;

public class LocalDiskStorageService : IStorageService
{
    private readonly string _uploadsPath;

    public LocalDiskStorageService()
    {
        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        if (!Directory.Exists(_uploadsPath)) Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string extension)
    {
        var uniqueFileName = Guid.NewGuid().ToString() + extension;
        var savePath = Path.Combine(_uploadsPath, uniqueFileName);

        using (var outStream = new FileStream(savePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outStream);
        }

        return $"/Uploads/{uniqueFileName}";
    }
}

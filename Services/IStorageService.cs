namespace iNaturalist_Lite.Services;

public interface IStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string extension);
}

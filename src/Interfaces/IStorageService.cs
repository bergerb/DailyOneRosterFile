namespace DailyOneRosterFile.Api.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(string fileName, byte[] content);
    Task<byte[]> DownloadFileAsync(string fileName);
    Task<string> GetLatestFileNameAsync();
}

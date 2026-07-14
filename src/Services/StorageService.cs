using DailyOneRosterFile.Api.Interfaces;

namespace DailyOneRosterFile.Api.Services;

public class StorageService(IBlobStorageService blobStorage) : IStorageService
{
    public Task<string> UploadFileAsync(string fileName, byte[] content)
    {
        return blobStorage.UploadFileAsync(fileName, content);
    }

    public Task<byte[]> DownloadFileAsync(string fileName)
    {
        return blobStorage.DownloadFileAsync(fileName);
    }

    public Task<string> GetLatestFileNameAsync()
    {
        return blobStorage.GetLatestFileNameAsync();
    }

    public Task<bool> FileExistsAsync(string key)
    {
        return blobStorage.FileExistsAsync(key);
    }
}

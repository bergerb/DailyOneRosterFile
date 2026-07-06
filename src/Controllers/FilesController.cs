using DailyOneRosterFile.Api.Attributes;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DailyOneRosterFile.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IOptions<StorageOptions> storageOptions, ITokenService tokenService, IStorageService storage) : ControllerBase
{
    private readonly string _storagePath = storageOptions.Value.GeneratedFilesPath;

    [HttpGet("get-token")]
    public async Task<IActionResult> GetDownloadToken()
    {
        string fileName = await GetLatestFileNameAsync();

        if (string.IsNullOrEmpty(fileName))
        {
            return NotFound("No files generated yet.");
        }

        var token = tokenService.GenerateToken(fileName);
        return Ok(new { token });
    }

    [HttpGet("latest-oneroster")]
    [ValidateToken]
    public async Task<IActionResult> DownloadLatestFile()
    {
        string fileName = await GetLatestFileNameAsync();

        if (string.IsNullOrEmpty(fileName))
        {
            return NotFound("No files generated yet.");
        }

        byte[] bytes = await DownloadFileAsync(fileName);

        return File(bytes, "application/zip", fileName);
    }

    #region Local Functions
    private async Task<string> GetLatestFileNameAsync()
    {
        if (storageOptions.Value.UseMinio)
        {
            return await storage.GetLatestFileNameAsync();
        }
        else
        {
            var files = Directory.GetFiles(_storagePath, "*.zip");
            if (files.Length == 0)
            {
                return string.Empty;
            }

            var latestFile = files
                .OrderByDescending(path => System.IO.File.GetLastWriteTimeUtc(path))
                .First();

            return Path.GetFileName(latestFile);
        }
    }

    private async Task<byte[]> DownloadFileAsync(string fileName)
    {
        if (storageOptions.Value.UseMinio)
        {
            return await storage.DownloadFileAsync(fileName);
        }
        else
        {
            var filePath = Path.Combine(_storagePath, fileName);
            return System.IO.File.ReadAllBytes(filePath);
        }
    }
    #endregion
}

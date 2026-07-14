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
        bool filesExist = await AnyFilesExistAsync();

        if (!filesExist)
        {
            return NotFound("No files generated yet.");
        }

        var token = tokenService.GenerateToken("OneRoster.zip");
        return Ok(new { token });
    }

    [HttpGet("latest-oneroster")]
    [ValidateToken]
    public async Task<IActionResult> DownloadLatestFile([FromQuery] int schoolCount = 22)
    {
        var variant = FileVariant.GetFolder(schoolCount);

        if (!await FileExistsForVariantAsync(variant))
        {
            return NotFound("No files generated yet.");
        }

        byte[] bytes = await DownloadFileAsync(variant);

        return File(bytes, "application/zip", "OneRoster.zip");
    }

    #region Local Functions
    private async Task<bool> AnyFilesExistAsync()
    {
        return await FileExistsForVariantAsync(FileVariant.Small) || await FileExistsForVariantAsync(FileVariant.Large);
    }

    private Task<bool> FileExistsForVariantAsync(string variant)
    {
        if (storageOptions.Value.UseMinio)
        {
            return storage.FileExistsAsync($"{variant}/OneRoster.zip");
        }

        var filePath = Path.Combine(_storagePath, variant, "OneRoster.zip");
        return Task.FromResult(System.IO.File.Exists(filePath));
    }

    private async Task<byte[]> DownloadFileAsync(string variant)
    {
        if (storageOptions.Value.UseMinio)
        {
            return await storage.DownloadFileAsync($"{variant}/OneRoster.zip");
        }
        else
        {
            var filePath = Path.Combine(_storagePath, variant, "OneRoster.zip");
            return System.IO.File.ReadAllBytes(filePath);
        }
    }
    #endregion
}

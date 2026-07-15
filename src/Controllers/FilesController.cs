using DailyOneRosterFile.Api.Attributes;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DailyOneRosterFile.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(
    IOptions<StorageOptions> storageOptions,
    ITokenService tokenService,
    IStorageService storage,
    IOneRosterValidator validator,
    IValidator<UploadFileDto> uploadValidator) : ControllerBase
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
    public async Task<IActionResult> DownloadLatestFile([FromQuery] string variant = FileVariant.Large)
    {
        if (variant != FileVariant.Small && variant != FileVariant.Large)
        {
            return BadRequest($"Invalid variant '{variant}'. Must be '{FileVariant.Small}' or '{FileVariant.Large}'.");
        }

        if (!await FileExistsForVariantAsync(variant))
        {
            return NotFound("No files generated yet.");
        }

        byte[] bytes = await DownloadFileAsync(variant);

        return File(bytes, "application/zip", "OneRoster.zip");
    }

    [HttpPost("validate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ValidateOneRosterFile([FromForm] UploadFileDto dto)
    {
        var validationResult = await uploadValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.First().ErrorMessage);
        }

        using var stream = dto.File!.OpenReadStream();
        var result = validator.Validate(stream);
        return Ok(result);
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

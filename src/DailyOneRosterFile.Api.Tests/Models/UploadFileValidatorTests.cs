using DailyOneRosterFile.Api.Models;
using Microsoft.AspNetCore.Http;

namespace DailyOneRosterFile.Api.Tests.Models;

public class UploadFileValidatorTests
{
    private readonly UploadFileValidator _validator = new();

    [Fact]
    public async Task File_Null_ReturnsError()
    {
        var dto = new UploadFileDto { File = null };
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "No file uploaded.");
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("file.csv")]
    [InlineData("archive.tar")]
    public async Task File_NotZipExtension_ReturnsError(string fileName)
    {
        var dto = new UploadFileDto { File = CreateFormFile(100, fileName) };
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Only .zip files are accepted.");
    }

    [Theory]
    [InlineData("OneRoster.zip")]
    [InlineData("oneroster.ZIP")]
    [InlineData("archive.zip")]
    public async Task File_ZipExtension_ReturnsNoFileNameError(string fileName)
    {
        var dto = new UploadFileDto { File = CreateFormFile(100, fileName) };
        var result = await _validator.ValidateAsync(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "File.FileName");
    }

    [Fact]
    public async Task File_ZeroLength_ReturnsError()
    {
        var dto = new UploadFileDto { File = CreateFormFile(0, "test.zip") };
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "No file uploaded.");
    }

    [Fact]
    public async Task File_Exceeds5MB_ReturnsError()
    {
        var dto = new UploadFileDto { File = CreateFormFile(5 * 1024 * 1024 + 1, "test.zip") };
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "File size must not exceed 5MB.");
    }

    [Fact]
    public async Task File_Exactly5MB_ReturnsNoSizeError()
    {
        var dto = new UploadFileDto { File = CreateFormFile(5 * 1024 * 1024, "test.zip") };
        var result = await _validator.ValidateAsync(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "File.Length");
    }

    [Fact]
    public async Task ValidInput_ReturnsValid()
    {
        var dto = new UploadFileDto { File = CreateFormFile(1024, "OneRoster.zip") };
        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    private static IFormFile CreateFormFile(long length, string fileName) =>
        new FormFile(new MemoryStream(new byte[length]), 0, length, "file", fileName);
}

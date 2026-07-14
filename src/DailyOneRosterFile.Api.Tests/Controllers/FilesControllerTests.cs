using DailyOneRosterFile.Api.Controllers;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyOneRosterFile.Api.Tests.Controllers;

public class FilesControllerTests
{
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<ITokenService> _tokenMock = new();

    private static IOptions<StorageOptions> CreateStorageOptions(bool useMinio = true) =>
        Options.Create(new StorageOptions { UseMinio = useMinio, GeneratedFilesPath = "GeneratedFiles" });

    private FilesController CreateController(bool useMinio = true) =>
        new(CreateStorageOptions(useMinio), _tokenMock.Object, _storageMock.Object);

    [Theory]
    [InlineData("invalid")]
    [InlineData("medium")]
    [InlineData("SMALL")]
    [InlineData("")]
    public async Task DownloadLatestFile_InvalidVariant_ReturnsBadRequest(string variant)
    {
        var controller = CreateController();

        var result = await controller.DownloadLatestFile(variant);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid variant", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task DownloadLatestFile_SmallVariant_NoFiles_ReturnsNotFound()
    {
        _storageMock
            .Setup(s => s.FileExistsAsync("small/OneRoster.zip"))
            .ReturnsAsync(false);

        var controller = CreateController();

        var result = await controller.DownloadLatestFile(FileVariant.Small);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadLatestFile_LargeVariant_NoFiles_ReturnsNotFound()
    {
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(false);

        var controller = CreateController();

        var result = await controller.DownloadLatestFile(FileVariant.Large);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadLatestFile_SmallVariant_FileExists_ReturnsFile()
    {
        byte[] expectedBytes = [1, 2, 3, 4];
        _storageMock
            .Setup(s => s.FileExistsAsync("small/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.DownloadFileAsync("small/OneRoster.zip"))
            .ReturnsAsync(expectedBytes);

        var controller = CreateController();

        var result = await controller.DownloadLatestFile(FileVariant.Small);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expectedBytes, fileResult.FileContents);
        Assert.Equal("application/zip", fileResult.ContentType);
        Assert.Equal("OneRoster.zip", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task DownloadLatestFile_LargeVariant_FileExists_ReturnsFile()
    {
        byte[] expectedBytes = [5, 6, 7, 8];
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.DownloadFileAsync("large/OneRoster.zip"))
            .ReturnsAsync(expectedBytes);

        var controller = CreateController();

        var result = await controller.DownloadLatestFile(FileVariant.Large);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expectedBytes, fileResult.FileContents);
    }

    [Fact]
    public async Task DownloadLatestFile_DefaultsToLarge()
    {
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.DownloadFileAsync("large/OneRoster.zip"))
            .ReturnsAsync([1]);

        var controller = CreateController();

        var result = await controller.DownloadLatestFile();

        Assert.IsType<FileContentResult>(result);
        _storageMock.Verify(s => s.FileExistsAsync("large/OneRoster.zip"), Times.Once);
    }

    [Fact]
    public async Task GetDownloadToken_NoFiles_ReturnsNotFound()
    {
        _storageMock
            .Setup(s => s.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var controller = CreateController();

        var result = await controller.GetDownloadToken();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDownloadToken_SmallFileExists_ReturnsToken()
    {
        _storageMock
            .Setup(s => s.FileExistsAsync("small/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(false);
        _tokenMock
            .Setup(t => t.GenerateToken("OneRoster.zip"))
            .Returns("test-token-123");

        var controller = CreateController();

        var result = await controller.GetDownloadToken();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var token = okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value);
        Assert.Equal("test-token-123", token);
    }

    [Fact]
    public async Task GetDownloadToken_LargeFileExists_ReturnsToken()
    {
        _storageMock
            .Setup(s => s.FileExistsAsync("small/OneRoster.zip"))
            .ReturnsAsync(false);
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(true);
        _tokenMock
            .Setup(t => t.GenerateToken("OneRoster.zip"))
            .Returns("test-token-456");

        var controller = CreateController();

        var result = await controller.GetDownloadToken();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var token = okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value);
        Assert.Equal("test-token-456", token);
    }
}

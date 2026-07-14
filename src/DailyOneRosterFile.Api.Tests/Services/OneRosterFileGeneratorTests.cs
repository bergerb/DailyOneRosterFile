using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using DailyOneRosterFile.Api.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyOneRosterFile.Api.Tests.Services;

public class OneRosterFileGeneratorTests : IDisposable
{
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly string _tempDir;

    public OneRosterFileGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"oneroster_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GenerateDailyFileAsync_LocalStorage_SavesToVariantSubfolder()
    {
        // Arrange
        var storageOptions = Options.Create(new StorageOptions
        {
            UseMinio = false,
            GeneratedFilesPath = _tempDir
        });
        var generator = new OneRosterFileGenerator(storageOptions, _storageMock.Object);

        // Act
        await generator.GenerateDailyFileAsync(FileVariant.Small, 3);

        // Assert
        var expectedPath = Path.Combine(_tempDir, FileVariant.Small, "OneRoster.zip");
        Assert.True(File.Exists(expectedPath), $"Expected file at {expectedPath}");
        Assert.True(new FileInfo(expectedPath).Length > 0, "File should not be empty");
    }

    [Fact]
    public async Task GenerateDailyFileAsync_LargeVariant_SavesToLargeSubfolder()
    {
        // Arrange
        var storageOptions = Options.Create(new StorageOptions
        {
            UseMinio = false,
            GeneratedFilesPath = _tempDir
        });
        var generator = new OneRosterFileGenerator(storageOptions, _storageMock.Object);

        // Act
        await generator.GenerateDailyFileAsync(FileVariant.Large, 22);

        // Assert
        var expectedPath = Path.Combine(_tempDir, FileVariant.Large, "OneRoster.zip");
        Assert.True(File.Exists(expectedPath), $"Expected file at {expectedPath}");
    }

    [Fact]
    public async Task GenerateDailyFileAsync_Minio_UploadsWithCorrectKey()
    {
        // Arrange
        var storageOptions = Options.Create(new StorageOptions
        {
            UseMinio = true,
            GeneratedFilesPath = _tempDir
        });
        _storageMock
            .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync("ok");
        var generator = new OneRosterFileGenerator(storageOptions, _storageMock.Object);

        // Act
        await generator.GenerateDailyFileAsync(FileVariant.Small, 3);

        // Assert
        _storageMock.Verify(
            s => s.UploadFileAsync(
                $"{FileVariant.Small}/OneRoster.zip",
                It.Is<byte[]>(b => b.Length > 0)),
            Times.Once);
    }

    [Fact]
    public async Task GenerateDailyFileAsync_Minio_LargeVariant_UploadsWithLargeKey()
    {
        // Arrange
        var storageOptions = Options.Create(new StorageOptions
        {
            UseMinio = true,
            GeneratedFilesPath = _tempDir
        });
        _storageMock
            .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync("ok");
        var generator = new OneRosterFileGenerator(storageOptions, _storageMock.Object);

        // Act
        await generator.GenerateDailyFileAsync(FileVariant.Large, 22);

        // Assert
        _storageMock.Verify(
            s => s.UploadFileAsync(
                $"{FileVariant.Large}/OneRoster.zip",
                It.Is<byte[]>(b => b.Length > 0)),
            Times.Once);
    }
}

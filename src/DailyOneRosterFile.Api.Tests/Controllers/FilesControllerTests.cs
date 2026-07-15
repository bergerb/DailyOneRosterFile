using DailyOneRosterFile.Api.Controllers;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using ValidationResult = DailyOneRosterFile.Api.Models.Validation.ValidationResult;

namespace DailyOneRosterFile.Api.Tests.Controllers;

public class FilesControllerTests
{
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<ITokenService> _tokenMock = new();
    private readonly Mock<IOneRosterValidator> _validatorMock = new();
    private readonly Mock<IValidator<UploadFileDto>> _uploadValidatorMock = new();

    private static IOptions<StorageOptions> CreateStorageOptions(bool useMinio = true) =>
        Options.Create(new StorageOptions { UseMinio = useMinio, GeneratedFilesPath = "GeneratedFiles" });

    private FilesController CreateController(bool useMinio = true) =>
        new(CreateStorageOptions(useMinio), _tokenMock.Object, _storageMock.Object, _validatorMock.Object, _uploadValidatorMock.Object);

    [Theory]
    [InlineData("invalid")]
    [InlineData("medium")]
    [InlineData("SMALL")]
    [InlineData("")]
    public async Task DownloadLatestFile_InvalidVariant_ReturnsBadRequest(string variant)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.DownloadLatestFile(variant);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid variant", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task DownloadLatestFile_SmallVariant_NoFiles_ReturnsNotFound()
    {
        // Arrange
        _storageMock
            .Setup(s => s.FileExistsAsync("small/OneRoster.zip"))
            .ReturnsAsync(false);
        var controller = CreateController();

        // Act
        var result = await controller.DownloadLatestFile(FileVariant.Small);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadLatestFile_LargeVariant_NoFiles_ReturnsNotFound()
    {
        // Arrange
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(false);
        var controller = CreateController();

        // Act
        var result = await controller.DownloadLatestFile(FileVariant.Large);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadLatestFile_SmallVariant_FileExists_ReturnsFile()
    {
        // Arrange
        byte[] expectedBytes = [1, 2, 3, 4];
        _storageMock
            .Setup(s => s.FileExistsAsync("small/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.DownloadFileAsync("small/OneRoster.zip"))
            .ReturnsAsync(expectedBytes);
        var controller = CreateController();

        // Act
        var result = await controller.DownloadLatestFile(FileVariant.Small);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expectedBytes, fileResult.FileContents);
        Assert.Equal("application/zip", fileResult.ContentType);
        Assert.Equal("OneRoster.zip", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task DownloadLatestFile_LargeVariant_FileExists_ReturnsFile()
    {
        // Arrange
        byte[] expectedBytes = [5, 6, 7, 8];
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.DownloadFileAsync("large/OneRoster.zip"))
            .ReturnsAsync(expectedBytes);
        var controller = CreateController();

        // Act
        var result = await controller.DownloadLatestFile(FileVariant.Large);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expectedBytes, fileResult.FileContents);
    }

    [Fact]
    public async Task DownloadLatestFile_DefaultsToLarge()
    {
        // Arrange
        _storageMock
            .Setup(s => s.FileExistsAsync("large/OneRoster.zip"))
            .ReturnsAsync(true);
        _storageMock
            .Setup(s => s.DownloadFileAsync("large/OneRoster.zip"))
            .ReturnsAsync([1]);
        var controller = CreateController();

        // Act
        var result = await controller.DownloadLatestFile();

        // Assert
        Assert.IsType<FileContentResult>(result);
        _storageMock.Verify(s => s.FileExistsAsync("large/OneRoster.zip"), Times.Once);
    }

    [Fact]
    public async Task GetDownloadToken_NoFiles_ReturnsNotFound()
    {
        // Arrange
        _storageMock
            .Setup(s => s.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        var controller = CreateController();

        // Act
        var result = await controller.GetDownloadToken();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDownloadToken_SmallFileExists_ReturnsToken()
    {
        // Arrange
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

        // Act
        var result = await controller.GetDownloadToken();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var token = okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value);
        Assert.Equal("test-token-123", token);
    }

    [Fact]
    public async Task GetDownloadToken_LargeFileExists_ReturnsToken()
    {
        // Arrange
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

        // Act
        var result = await controller.GetDownloadToken();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var token = okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value);
        Assert.Equal("test-token-456", token);
    }

    [Fact]
    public async Task ValidateOneRosterFile_NoFile_ReturnsBadRequest()
    {
        // Arrange
        _uploadValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UploadFileDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new ValidationFailure("File", "No file uploaded.")]));
        var controller = CreateController();

        // Act
        var result = await controller.ValidateOneRosterFile(new UploadFileDto());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateOneRosterFile_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        _uploadValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UploadFileDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new ValidationFailure("File.Size", "No file uploaded.")]));
        var dto = new UploadFileDto { File = new FormFile(new MemoryStream(), 0, 0, "file", "test.zip") };
        var controller = CreateController();

        // Act
        var result = await controller.ValidateOneRosterFile(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateOneRosterFile_NonZipFile_ReturnsBadRequest()
    {
        // Arrange
        _uploadValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UploadFileDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new ValidationFailure("File.FileName", "Only .zip files are accepted.")]));
        var dto = new UploadFileDto { File = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", "test.txt") };
        var controller = CreateController();

        // Act
        var result = await controller.ValidateOneRosterFile(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(".zip", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task ValidateOneRosterFile_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        _uploadValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UploadFileDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new ValidationFailure("File.Size", "File size must not exceed 5MB.")]));
        var oversizedContent = new byte[5 * 1024 * 1024 + 1]; // 5MB + 1 byte
        var dto = new UploadFileDto { File = new FormFile(new MemoryStream(oversizedContent), 0, oversizedContent.Length, "file", "OneRoster.zip") };
        var controller = CreateController();

        // Act
        var result = await controller.ValidateOneRosterFile(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("5MB", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task ValidateOneRosterFile_ValidZip_ReturnsValidationResult()
    {
        // Arrange
        _uploadValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UploadFileDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        var validationResult = new DailyOneRosterFile.Api.Models.Validation.ValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = [],
            ValidatedAt = DateTimeOffset.UtcNow
        };
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(validationResult);

        var fileContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // PK header
        var dto = new UploadFileDto { File = new FormFile(new MemoryStream(fileContent), 0, fileContent.Length, "file", "OneRoster.zip") };
        var controller = CreateController();

        // Act
        var result = await controller.ValidateOneRosterFile(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<DailyOneRosterFile.Api.Models.Validation.ValidationResult>(okResult.Value);
        Assert.True(returnedResult.IsValid);
    }
}

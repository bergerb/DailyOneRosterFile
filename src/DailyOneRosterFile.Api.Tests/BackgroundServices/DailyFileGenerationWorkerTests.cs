using DailyOneRosterFile.Api.BackgroundServices;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyOneRosterFile.Api.Tests.BackgroundServices;

public class DailyFileGenerationWorkerTests
{
    private readonly Mock<IOneRosterFileGenerator> _generatorMock = new();
    private readonly Mock<ILogger<DailyFileGenerationWorker>> _loggerMock = new();

    private static IOptions<FileVariantOptions> CreateVariantOptions(int small = 3, int large = 22) =>
        Options.Create(new FileVariantOptions { SmallSchoolCount = small, LargeSchoolCount = large });

    [Fact]
    public async Task ExecuteAsync_CallsGeneratorWithSmallAndLargeVariants()
    {
        // Arrange
        _generatorMock
            .Setup(g => g.GenerateDailyFileAsync(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        var worker = new DailyFileGenerationWorker(
            _generatorMock.Object,
            CreateVariantOptions(),
            _loggerMock.Object);

        // Act
        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);
        await task;

        // Assert
        _generatorMock.Verify(
            g => g.GenerateDailyFileAsync(FileVariant.Small, 3), Times.Once);
        _generatorMock.Verify(
            g => g.GenerateDailyFileAsync(FileVariant.Large, 22), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfiguredSchoolCounts()
    {
        // Arrange
        _generatorMock
            .Setup(g => g.GenerateDailyFileAsync(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        var worker = new DailyFileGenerationWorker(
            _generatorMock.Object,
            CreateVariantOptions(small: 5, large: 50),
            _loggerMock.Object);

        // Act
        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);
        await task;

        // Assert
        _generatorMock.Verify(
            g => g.GenerateDailyFileAsync(FileVariant.Small, 5), Times.Once);
        _generatorMock.Verify(
            g => g.GenerateDailyFileAsync(FileVariant.Large, 50), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratorThrows_SmallOnly_CallsSmallAndLogsError()
    {
        // Arrange
        _generatorMock
            .Setup(g => g.GenerateDailyFileAsync(FileVariant.Small, It.IsAny<int>()))
            .ThrowsAsync(new Exception("Generation failed"));
        var worker = new DailyFileGenerationWorker(
            _generatorMock.Object,
            CreateVariantOptions(),
            _loggerMock.Object);

        // Act
        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);
        await task;

        // Assert
        _generatorMock.Verify(
            g => g.GenerateDailyFileAsync(FileVariant.Small, 3), Times.Once);
        _generatorMock.Verify(
            g => g.GenerateDailyFileAsync(FileVariant.Large, 22), Times.Never);
    }
}

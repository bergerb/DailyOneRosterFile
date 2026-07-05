using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Models;
using Backend.Services;
using Cronos;

namespace Backend.BackgroundServices
{
    public class DailyFileGenerationWorker : BackgroundService
    {
        private readonly IOneRosterFileGenerator _generator;
        private readonly ILogger<DailyFileGenerationWorker> _logger;
        private readonly string _storagePath;

        public DailyFileGenerationWorker(
            IOneRosterFileGenerator generator,
            ILogger<DailyFileGenerationWorker> logger,
            IOptions<StorageOptions> storageOptions)
        {
            _generator = generator;
            _logger = logger;
            _storagePath = storageOptions.Value.GeneratedFilesPath;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Daily File Generation Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting daily file generation...");
                    var generatedPath = await _generator.GenerateDailyFileAsync();
                    DeleteOldZipFiles(generatedPath);
                    _logger.LogInformation("Daily file generation completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during daily file generation.");
                }

                // Calculate next occurrence at local midnight using Cronos and wait until then.
                try
                {
                    var nowUtc = DateTime.UtcNow;
                    var nextUtc = CronExpression.Parse("0 0 * * *").GetNextOccurrence(nowUtc, TimeZoneInfo.Local);

                    if (nextUtc.HasValue)
                    {
                        var delay = nextUtc.Value - DateTime.UtcNow;
                        if (delay < TimeSpan.Zero)
                        {
                            delay = TimeSpan.Zero;
                        }

                        var nextLocal = TimeZoneInfo.ConvertTimeFromUtc(nextUtc.Value, TimeZoneInfo.Local);
                        _logger.LogInformation("Next daily file generation scheduled at {NextRunLocal}", nextLocal);

                        await Task.Delay(delay, stoppingToken);
                    }
                    else
                    {
                        _logger.LogWarning("Cron expression did not yield a next occurrence; falling back to 24h delay.");
                        await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Shutdown requested - exit loop
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while waiting for next scheduled run. Will retry after 1 hour.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private void DeleteOldZipFiles(string keepPath)
        {
            var files = Directory.GetFiles(_storagePath, "*.zip");
            foreach (var file in files.Where(path => !string.Equals(path, keepPath, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed deleting old zip file {FilePath}", file);
                }
            }
        }
    }
}
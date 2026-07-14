using Cronos;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.Extensions.Options;

namespace DailyOneRosterFile.Api.BackgroundServices;

public class DailyFileGenerationWorker(
    IOneRosterFileGenerator generator,
    IOptions<FileVariantOptions> variantOptions,
    ILogger<DailyFileGenerationWorker> logger) : BackgroundService
{
    private readonly IOneRosterFileGenerator _generator = generator;
    private readonly FileVariantOptions _variantOptions = variantOptions.Value;
    private readonly ILogger<DailyFileGenerationWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily File Generation Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting daily file generation...");
                await _generator.GenerateDailyFileAsync(FileVariant.Small, _variantOptions.SmallSchoolCount);
                await _generator.GenerateDailyFileAsync(FileVariant.Large, _variantOptions.LargeSchoolCount);
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
}
using Microsoft.EntityFrameworkCore;
using SmashPointBookingAPI.Data;

namespace SmashPointBookingAPI.Services;

public sealed class SecurityRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _services;
    private readonly ILogger<SecurityRetentionWorker> _logger;

    public SecurityRetentionWorker(
        IServiceScopeFactory services,
        ILogger<SecurityRetentionWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup, then once per day
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Null out raw IPs and User Agents after 90 days for privacy compliance
                var ipPruned = await db.Database.ExecuteSqlRawAsync(
                    @"UPDATE security_events
                      SET ""IpAddress"" = NULL, ""UserAgent"" = NULL
                      WHERE ""OccurredAt"" < now() - interval '90 days'
                        AND (""IpAddress"" IS NOT NULL OR ""UserAgent"" IS NOT NULL);",
                    stoppingToken);

                // 2. Delete info-level events after 180 days to save database space
                var infoDeleted = await db.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM security_events
                      WHERE ""Severity"" = 'info'
                        AND ""OccurredAt"" < now() - interval '180 days';",
                    stoppingToken);

                if (ipPruned > 0 || infoDeleted > 0)
                {
                    _logger.LogInformation(
                        "Security retention: pruned {Pruned} IPs, deleted {Deleted} info events",
                        ipPruned, infoDeleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Security retention worker failed");
            }

            // Sleep for 24 hours before running again
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class MonitoringCollectorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options) : BackgroundService
{
    private readonly MonitoringOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.SampleIntervalMilliseconds));
        var lastFlush = Stopwatch.GetTimestamp();
        var lastTrim = Stopwatch.GetTimestamp();
        var flushInterval = TimeSpan.FromMilliseconds(_options.FlushIntervalMilliseconds);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var collector = scope.ServiceProvider.GetRequiredService<ISystemMetricsCollector>();
            await collector.CollectAsync(stoppingToken);

            var persistence = scope.ServiceProvider.GetRequiredService<IMetricPersistenceService>();
            if (Stopwatch.GetElapsedTime(lastFlush) >= flushInterval)
            {
                await persistence.FlushAsync(stoppingToken);
                lastFlush = Stopwatch.GetTimestamp();
            }

            if (Stopwatch.GetElapsedTime(lastTrim) >= TimeSpan.FromMinutes(10))
            {
                await persistence.TrimAsync(stoppingToken);
                lastTrim = Stopwatch.GetTimestamp();
            }
        }
    }
}

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class MonitoringCollectorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<MonitoringCollectorHostedService> logger) : BackgroundService
{
    private readonly MonitoringOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.SampleIntervalMilliseconds));
        var lastFlush = Stopwatch.GetTimestamp();
        var lastTrim = Stopwatch.GetTimestamp();
        long? lastServerSample = null;
        long? lastDomainProbe = null;
        Task? domainProbeTask = null;
        var flushInterval = TimeSpan.FromMilliseconds(_options.FlushIntervalMilliseconds);
        var serverInterval = TimeSpan.FromMilliseconds(_options.ServerSampleIntervalMilliseconds);
        var domainInterval = TimeSpan.FromMilliseconds(_options.DomainProbeIntervalMilliseconds);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var collector = scope.ServiceProvider.GetRequiredService<ISystemMetricsCollector>();
            await collector.CollectAsync(stoppingToken);

            if (lastServerSample is null || Stopwatch.GetElapsedTime(lastServerSample.Value) >= serverInterval)
            {
                var serverCollector = scope.ServiceProvider.GetRequiredService<IServerMonitoringCollector>();
                await serverCollector.CollectServerAsync(stoppingToken);
                lastServerSample = Stopwatch.GetTimestamp();
            }

            if (domainProbeTask is { IsCompleted: true })
            {
                if (domainProbeTask.IsFaulted)
                    logger.LogWarning(domainProbeTask.Exception, "بررسی دامنه‌ها با خطا تمام شد.");
                domainProbeTask = null;
            }

            if (domainProbeTask is null &&
                (lastDomainProbe is null || Stopwatch.GetElapsedTime(lastDomainProbe.Value) >= domainInterval))
            {
                lastDomainProbe = Stopwatch.GetTimestamp();
                domainProbeTask = ProbeDomainsInBackgroundAsync(stoppingToken);
            }

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

    private async Task ProbeDomainsInBackgroundAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var serverCollector = scope.ServiceProvider.GetRequiredService<IServerMonitoringCollector>();
        await serverCollector.ProbeDomainsAsync(stoppingToken);
        var persistence = scope.ServiceProvider.GetRequiredService<IMetricPersistenceService>();
        await persistence.FlushAsync(stoppingToken);
    }
}

using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Application.Monitoring;

public sealed class SystemMetricsCollector(ISystemSampleProbe probe, IMonitoringService monitoringService) : ISystemMetricsCollector
{
    public Task CollectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        monitoringService.RecordSample(probe.Capture());
        return Task.CompletedTask;
    }
}

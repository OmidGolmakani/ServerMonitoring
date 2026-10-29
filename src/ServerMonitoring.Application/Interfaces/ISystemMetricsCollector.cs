namespace ServerMonitoring.Application.Interfaces;

public interface ISystemMetricsCollector
{
    Task CollectAsync(CancellationToken cancellationToken);
}

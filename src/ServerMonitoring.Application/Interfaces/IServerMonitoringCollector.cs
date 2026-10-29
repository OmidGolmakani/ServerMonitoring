namespace ServerMonitoring.Application.Interfaces;

public interface IServerMonitoringCollector
{
    Task CollectServerAsync(CancellationToken cancellationToken);

    Task ProbeDomainsAsync(CancellationToken cancellationToken);
}

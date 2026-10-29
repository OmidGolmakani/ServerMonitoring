namespace ServerMonitoring.Application.Interfaces;

public interface IMetricPersistenceService
{
    Task LoadHistoryAsync(CancellationToken cancellationToken);

    Task FlushAsync(CancellationToken cancellationToken);

    Task TrimAsync(CancellationToken cancellationToken);
}

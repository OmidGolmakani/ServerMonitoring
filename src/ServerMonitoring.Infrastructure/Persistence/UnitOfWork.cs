using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Infrastructure.Persistence;

public sealed class UnitOfWork(
    IRequestTraceRepository requestTraces,
    ISystemSampleRepository systemSamples,
    IMonitoringDatabase database,
    MonitoringStore store) : IUnitOfWork
{
    public IRequestTraceRepository RequestTraces { get; } = requestTraces;

    public ISystemSampleRepository SystemSamples { get; } = systemSamples;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        await store.FlushLock.WaitAsync(cancellationToken);
        try
        {
            if (!store.HasPending)
                return 0;

            var snapshot = store.Capture();
            await database.WriteAsync(snapshot.Requests, snapshot.Samples, cancellationToken);
            store.ClearPending();
            return snapshot.Requests.Count + snapshot.Samples.Count;
        }
        finally
        {
            store.FlushLock.Release();
        }
    }
}

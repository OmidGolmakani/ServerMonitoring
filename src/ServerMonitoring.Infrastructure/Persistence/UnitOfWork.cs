using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Infrastructure.Persistence;

public sealed class UnitOfWork(
    IRequestTraceRepository requestTraces,
    ISystemSampleRepository systemSamples,
    IServerSampleRepository serverSamples,
    IDomainCheckRepository domainChecks,
    IMonitoringDatabase database,
    MonitoringStore store) : IUnitOfWork
{
    public IRequestTraceRepository RequestTraces { get; } = requestTraces;

    public ISystemSampleRepository SystemSamples { get; } = systemSamples;

    public IServerSampleRepository ServerSamples { get; } = serverSamples;

    public IDomainCheckRepository DomainChecks { get; } = domainChecks;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        await store.FlushLock.WaitAsync(cancellationToken);
        try
        {
            if (!store.HasPending)
                return 0;

            var snapshot = store.Capture();
            await database.WriteAsync(
                snapshot.Requests,
                snapshot.Samples,
                snapshot.ServerSamples,
                snapshot.DomainChecks,
                cancellationToken);
            store.ClearPending();
            return snapshot.Requests.Count
                + snapshot.Samples.Count
                + snapshot.ServerSamples.Count
                + snapshot.DomainChecks.Count;
        }
        finally
        {
            store.FlushLock.Release();
        }
    }
}

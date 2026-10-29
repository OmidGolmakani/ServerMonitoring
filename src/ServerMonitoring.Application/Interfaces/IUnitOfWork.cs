namespace ServerMonitoring.Application.Interfaces;

public interface IUnitOfWork
{
    IRequestTraceRepository RequestTraces { get; }

    ISystemSampleRepository SystemSamples { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

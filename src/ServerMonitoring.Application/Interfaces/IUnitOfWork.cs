namespace ServerMonitoring.Application.Interfaces;

public interface IUnitOfWork
{
    IRequestTraceRepository RequestTraces { get; }

    ISystemSampleRepository SystemSamples { get; }

    IServerSampleRepository ServerSamples { get; }

    IDomainCheckRepository DomainChecks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

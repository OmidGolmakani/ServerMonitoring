using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Interfaces;

public interface IMonitoringDatabase
{
    string FilePath { get; }

    Task<IReadOnlyList<RequestTrace>> ReadRequestsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SystemSample>> ReadSamplesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ServerSample>> ReadServerSamplesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DomainCheck>> ReadDomainChecksAsync(CancellationToken cancellationToken);

    Task WriteAsync(
        IReadOnlyList<RequestTrace> requests,
        IReadOnlyList<SystemSample> samples,
        IReadOnlyList<ServerSample> serverSamples,
        IReadOnlyList<DomainCheck> domainChecks,
        CancellationToken cancellationToken);
}

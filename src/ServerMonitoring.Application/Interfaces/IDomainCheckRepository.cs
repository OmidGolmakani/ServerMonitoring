using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Interfaces;

public interface IDomainCheckRepository
{
    void Stage(DomainCheck check);

    Task<IReadOnlyList<DomainCheck>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<DomainCheck>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken);

    void Seed(IReadOnlyList<DomainCheck> checks);

    void TrimLive(DateTime thresholdUtc);
}

using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Infrastructure.Persistence;

namespace ServerMonitoring.Infrastructure.Repositories;

public sealed class DomainCheckRepository(MonitoringStore store, IMonitoringDatabase database) : IDomainCheckRepository
{
    public void Stage(DomainCheck check) => store.AddDomainCheck(check);

    public Task<IReadOnlyList<DomainCheck>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.QueryDomainChecks(fromUtc));
    }

    public async Task<IReadOnlyList<DomainCheck>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken)
    {
        var checks = await database.ReadDomainChecksAsync(cancellationToken);
        var selected = checks
            .Where(check => check.CheckedAtUtc >= fromUtc)
            .OrderByDescending(check => check.CheckedAtUtc)
            .Take(limit)
            .ToList();

        selected.Reverse();
        return selected;
    }

    public void Seed(IReadOnlyList<DomainCheck> checks) => store.SeedDomainChecks(checks);

    public void TrimLive(DateTime thresholdUtc) => store.TrimDomainChecks(thresholdUtc);
}

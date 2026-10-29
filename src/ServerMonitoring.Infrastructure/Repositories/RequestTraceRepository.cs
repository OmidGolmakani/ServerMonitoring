using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Infrastructure.Persistence;

namespace ServerMonitoring.Infrastructure.Repositories;

public sealed class RequestTraceRepository(MonitoringStore store, IMonitoringDatabase database) : IRequestTraceRepository
{
    public long DroppedObservations => store.DroppedObservations;

    public void Stage(RequestTrace trace) => store.AddRequest(trace);

    public Task<IReadOnlyList<RequestTrace>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.QueryRequests(fromUtc));
    }

    public async Task<IReadOnlyList<RequestTrace>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken)
    {
        var traces = await database.ReadRequestsAsync(cancellationToken);
        var selected = traces
            .Where(trace => trace.OccurredAtUtc >= fromUtc)
            .OrderByDescending(trace => trace.OccurredAtUtc)
            .Take(limit)
            .ToList();

        selected.Reverse();
        return selected;
    }

    public void Seed(IReadOnlyList<RequestTrace> traces) => store.SeedRequests(traces);

    public void TrimLive(DateTime thresholdUtc) => store.TrimRequests(thresholdUtc);
}

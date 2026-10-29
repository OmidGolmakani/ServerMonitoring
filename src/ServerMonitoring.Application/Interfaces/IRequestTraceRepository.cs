using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Interfaces;

public interface IRequestTraceRepository
{
    long DroppedObservations { get; }

    void Stage(RequestTrace trace);

    Task<IReadOnlyList<RequestTrace>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestTrace>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken);

    void Seed(IReadOnlyList<RequestTrace> traces);

    void TrimLive(DateTime thresholdUtc);
}

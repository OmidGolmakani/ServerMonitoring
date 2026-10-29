using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Interfaces;

public interface ISystemSampleRepository
{
    void Stage(SystemSample sample);

    Task<IReadOnlyList<SystemSample>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<SystemSample>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken);

    void Seed(IReadOnlyList<SystemSample> samples);

    void TrimLive(DateTime thresholdUtc);
}

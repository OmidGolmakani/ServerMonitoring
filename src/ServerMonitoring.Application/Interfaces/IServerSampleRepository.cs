using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Interfaces;

public interface IServerSampleRepository
{
    void Stage(ServerSample sample);

    Task<IReadOnlyList<ServerSample>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServerSample>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken);

    void Seed(IReadOnlyList<ServerSample> samples);

    void TrimLive(DateTime thresholdUtc);
}

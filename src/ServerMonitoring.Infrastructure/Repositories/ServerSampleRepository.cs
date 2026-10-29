using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Infrastructure.Persistence;

namespace ServerMonitoring.Infrastructure.Repositories;

public sealed class ServerSampleRepository(MonitoringStore store, IMonitoringDatabase database) : IServerSampleRepository
{
    public void Stage(ServerSample sample) => store.AddServerSample(sample);

    public Task<IReadOnlyList<ServerSample>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.QueryServerSamples(fromUtc));
    }

    public async Task<IReadOnlyList<ServerSample>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken)
    {
        var samples = await database.ReadServerSamplesAsync(cancellationToken);
        var selected = samples
            .Where(sample => sample.CapturedAtUtc >= fromUtc)
            .OrderByDescending(sample => sample.CapturedAtUtc)
            .Take(limit)
            .ToList();

        selected.Reverse();
        return selected;
    }

    public void Seed(IReadOnlyList<ServerSample> samples) => store.SeedServerSamples(samples);

    public void TrimLive(DateTime thresholdUtc) => store.TrimServerSamples(thresholdUtc);
}

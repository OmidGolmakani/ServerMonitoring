using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Infrastructure.Persistence;

namespace ServerMonitoring.Infrastructure.Repositories;

public sealed class SystemSampleRepository(MonitoringStore store, IMonitoringDatabase database) : ISystemSampleRepository
{
    public void Stage(SystemSample sample) => store.AddSample(sample);

    public Task<IReadOnlyList<SystemSample>> GetSinceAsync(DateTime fromUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.QuerySamples(fromUtc));
    }

    public async Task<IReadOnlyList<SystemSample>> ReadHistoryAsync(DateTime fromUtc, int limit, CancellationToken cancellationToken)
    {
        var samples = await database.ReadSamplesAsync(cancellationToken);
        var selected = samples
            .Where(sample => sample.CapturedAtUtc >= fromUtc)
            .OrderByDescending(sample => sample.CapturedAtUtc)
            .Take(limit)
            .ToList();

        selected.Reverse();
        return selected;
    }

    public void Seed(IReadOnlyList<SystemSample> samples) => store.SeedSamples(samples);

    public void TrimLive(DateTime thresholdUtc) => store.TrimSamples(thresholdUtc);
}

using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class MetricPersistenceService(IUnitOfWork unitOfWork, IOptions<MonitoringOptions> options) : IMetricPersistenceService
{
    private readonly MonitoringOptions _options = options.Value;

    public async Task LoadHistoryAsync(CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.UtcNow.AddHours(-_options.RetentionHours);
        var traces = await unitOfWork.RequestTraces.ReadHistoryAsync(fromUtc, _options.LiveRequestCapacity, cancellationToken);
        var samples = await unitOfWork.SystemSamples.ReadHistoryAsync(fromUtc, _options.LiveSampleCapacity, cancellationToken);
        var serverSamples = await unitOfWork.ServerSamples.ReadHistoryAsync(fromUtc, _options.LiveServerSampleCapacity, cancellationToken);
        var domainChecks = await unitOfWork.DomainChecks.ReadHistoryAsync(fromUtc, _options.LiveDomainCheckCapacity, cancellationToken);
        unitOfWork.RequestTraces.Seed(traces);
        unitOfWork.SystemSamples.Seed(samples);
        unitOfWork.ServerSamples.Seed(serverSamples);
        unitOfWork.DomainChecks.Seed(domainChecks);
    }

    public Task FlushAsync(CancellationToken cancellationToken)
        => unitOfWork.SaveChangesAsync(cancellationToken);

    public async Task TrimAsync(CancellationToken cancellationToken)
    {
        var thresholdUtc = DateTime.UtcNow.AddHours(-_options.RetentionHours);
        unitOfWork.RequestTraces.TrimLive(thresholdUtc);
        unitOfWork.SystemSamples.TrimLive(thresholdUtc);
        unitOfWork.ServerSamples.TrimLive(thresholdUtc);
        unitOfWork.DomainChecks.TrimLive(thresholdUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

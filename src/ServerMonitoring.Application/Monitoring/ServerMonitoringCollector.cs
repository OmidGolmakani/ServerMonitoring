using AutoMapper;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Monitoring;

public sealed class ServerMonitoringCollector(
    IServerMetricsProbe serverMetricsProbe,
    IDomainCatalog domainCatalog,
    IDomainHealthProbe domainHealthProbe,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IOptions<MonitoringOptions> options) : IServerMonitoringCollector
{
    private readonly MonitoringOptions _options = options.Value;

    public Task CollectServerAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sample = mapper.Map<ServerSample>(serverMetricsProbe.Capture());
        unitOfWork.ServerSamples.Stage(sample);
        return Task.CompletedTask;
    }

    public async Task ProbeDomainsAsync(CancellationToken cancellationToken)
    {
        var targets = domainCatalog.GetTargets();
        if (targets.Count == 0)
            return;

        var gate = new SemaphoreSlim(_options.DomainProbeMaxConcurrency, _options.DomainProbeMaxConcurrency);
        var tasks = targets.Select(async target =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var result = await domainHealthProbe.CheckAsync(target, cancellationToken);
                unitOfWork.DomainChecks.Stage(mapper.Map<DomainCheck>(result));
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}

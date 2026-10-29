using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IMonitoringService
{
    void RecordRequest(NewRequestObservationDto observation);

    void RecordSample(NewSystemSampleDto sample);

    Task<DashboardDto> GetDashboardAsync(int? windowMinutes, CancellationToken cancellationToken);

    Task<MonitoringOverviewDto> GetOverviewAsync(int? windowMinutes, CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteMetricDto>> GetRoutesAsync(int? windowMinutes, CancellationToken cancellationToken);

    Task<IReadOnlyList<SystemSampleDto>> GetSamplesAsync(int? windowMinutes, CancellationToken cancellationToken);

    Task<ServerOverviewDto> GetServerAsync(int? windowMinutes, CancellationToken cancellationToken);

    Task<IReadOnlyList<DomainStatusDto>> GetDomainsAsync(int? windowMinutes, CancellationToken cancellationToken);

    void RecordServerSample(NewServerSampleDto sample);

    void RecordDomainCheck(NewDomainCheckDto check);

    Task<string> BuildHtmlReportAsync(int? windowMinutes, CancellationToken cancellationToken);
}

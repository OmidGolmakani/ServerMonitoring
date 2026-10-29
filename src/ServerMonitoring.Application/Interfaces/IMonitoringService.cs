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

    Task<string> BuildHtmlReportAsync(int? windowMinutes, CancellationToken cancellationToken);
}

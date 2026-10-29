namespace ServerMonitoring.Application.Dtos;

public sealed class DashboardDto
{
    public MonitoringOverviewDto Overview { get; init; } = new();

    public IReadOnlyList<RouteMetricDto> Routes { get; init; } = [];

    public IReadOnlyList<SystemSampleDto> Samples { get; init; } = [];

    public IReadOnlyList<StatusShareDto> StatusShares { get; init; } = [];

    public IReadOnlyList<RecentRequestDto> RecentRequests { get; init; } = [];

    public IReadOnlyList<MonitoringHighlightDto> Highlights { get; init; } = [];

    public ServerOverviewDto Server { get; init; } = new();

    public IReadOnlyList<ServerSampleDto> ServerSamples { get; init; } = [];

    public IReadOnlyList<DomainStatusDto> Domains { get; init; } = [];
}

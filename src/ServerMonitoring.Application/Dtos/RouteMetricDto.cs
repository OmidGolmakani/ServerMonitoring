namespace ServerMonitoring.Application.Dtos;

public sealed class RouteMetricDto
{
    public string Route { get; init; } = string.Empty;

    public string HttpMethod { get; init; } = string.Empty;

    public long RequestCount { get; init; }

    public long ErrorCount { get; init; }

    public double AverageElapsedMilliseconds { get; init; }

    public double P95ElapsedMilliseconds { get; init; }

    public double MaxElapsedMilliseconds { get; init; }

    public double TotalCpuMilliseconds { get; init; }

    public double AverageCpuMilliseconds { get; init; }

    public double CpuSharePercent { get; init; }

    public long TotalAllocatedBytes { get; init; }

    public double AllocationSharePercent { get; init; }

    public long TotalResponseBytes { get; init; }

    public double RequestsPerSecond { get; init; }
}

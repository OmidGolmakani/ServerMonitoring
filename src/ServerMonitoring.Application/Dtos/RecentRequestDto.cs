namespace ServerMonitoring.Application.Dtos;

public sealed class RecentRequestDto
{
    public string Route { get; init; } = string.Empty;

    public string HttpMethod { get; init; } = string.Empty;

    public int StatusCode { get; init; }

    public double ElapsedMilliseconds { get; init; }

    public double CpuMilliseconds { get; init; }

    public long AllocatedBytes { get; init; }

    public long ResponseBytes { get; init; }

    public DateTime OccurredAtUtc { get; init; }
}

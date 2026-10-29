namespace ServerMonitoring.Domain.Entities;

public sealed class RequestTrace
{
    public long Id { get; set; }

    public string Route { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public double ElapsedMilliseconds { get; set; }

    public double CpuMilliseconds { get; set; }

    public long AllocatedBytes { get; set; }

    public long ResponseBytes { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}

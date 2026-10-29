namespace ServerMonitoring.Application.Dtos;

public sealed class DomainStatusDto
{
    public string Name { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public bool IsUp { get; init; }

    public int? StatusCode { get; init; }

    public double ElapsedMilliseconds { get; init; }

    public DateTime? LastCheckedAtUtc { get; init; }

    public string? Error { get; init; }

    public double UpPercent { get; init; }

    public long CheckCount { get; init; }
}

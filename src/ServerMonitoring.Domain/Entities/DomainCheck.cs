namespace ServerMonitoring.Domain.Entities;

public sealed class DomainCheck
{
    public long Id { get; set; }

    public DateTime CheckedAtUtc { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsUp { get; set; }

    public int? StatusCode { get; set; }

    public double ElapsedMilliseconds { get; set; }

    public string? Error { get; set; }
}

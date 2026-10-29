namespace ServerMonitoring.Application.Dtos;

public sealed class NewDomainCheckDto
{
    public DateTime CheckedAtUtc { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public bool IsUp { get; init; }

    public int? StatusCode { get; init; }

    public double ElapsedMilliseconds { get; init; }

    public string? Error { get; init; }
}

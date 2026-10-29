namespace ServerMonitoring.Application.Dtos;

public sealed class StatusShareDto
{
    public int StatusCode { get; init; }

    public long Count { get; init; }

    public double Percent { get; init; }
}

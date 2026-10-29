namespace ServerMonitoring.Application.Dtos;

public sealed class MonitoringHighlightDto
{
    public string Title { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Detail { get; init; } = string.Empty;
}

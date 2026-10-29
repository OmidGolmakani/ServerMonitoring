namespace ServerMonitoring.Application.Dtos;

public sealed class DiscoveredDomainDto
{
    public string Name { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public bool Selected { get; init; }
}

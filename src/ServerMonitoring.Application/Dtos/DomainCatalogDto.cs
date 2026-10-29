namespace ServerMonitoring.Application.Dtos;

public sealed class DomainCatalogDto
{
    public string IisConfigPath { get; init; } = string.Empty;

    public bool IisConfigExists { get; init; }

    public bool IisConfigReadable { get; init; }

    public string? DiscoveryError { get; init; }

    public int DiscoveredCount { get; init; }

    public int SelectedCount { get; init; }

    public IReadOnlyList<DiscoveredDomainDto> Items { get; init; } = [];
}

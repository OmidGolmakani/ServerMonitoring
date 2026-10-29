namespace ServerMonitoring.Application.Dtos;

public sealed class SaveDomainSelectionRequestDto
{
    public IReadOnlyList<string> SelectedUrls { get; init; } = [];
}

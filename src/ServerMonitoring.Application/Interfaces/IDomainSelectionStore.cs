namespace ServerMonitoring.Application.Interfaces;

public interface IDomainSelectionStore
{
    IReadOnlySet<string> GetSelectedUrls();

    Task SaveSelectedUrlsAsync(IReadOnlyList<string> urls, CancellationToken cancellationToken);
}

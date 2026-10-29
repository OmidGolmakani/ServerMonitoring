using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Monitoring;

public sealed class DomainCatalogService(
    IDomainCatalog domainCatalog,
    IDomainSelectionStore selectionStore) : IDomainCatalogService
{
    public DomainCatalogDto GetCatalog() => domainCatalog.GetCatalog();

    public async Task<DomainCatalogDto> SaveSelectionAsync(
        SaveDomainSelectionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.SelectedUrls is null)
            throw new ValidationException("لیست انتخاب دامنه نامعتبر است.");

        var cleaned = request.SelectedUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var url in cleaned)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ValidationException(
                    "آدرس دامنه نامعتبر است.",
                    new Dictionary<string, string[]>
                    {
                        ["selectedUrls"] = [$"آدرس نامعتبر: {url}"]
                    });
            }
        }

        await selectionStore.SaveSelectedUrlsAsync(cleaned, cancellationToken);
        domainCatalog.InvalidateCache();
        return domainCatalog.GetCatalog();
    }
}

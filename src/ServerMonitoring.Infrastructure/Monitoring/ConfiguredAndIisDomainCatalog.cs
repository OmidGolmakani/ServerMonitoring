using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class ConfiguredAndIisDomainCatalog(
    IOptions<MonitoringOptions> options,
    IDomainSelectionStore selectionStore,
    IServer server,
    IisRuntimeSiteDiscoverer iisDiscoverer) : IDomainCatalog
{
    private readonly MonitoringOptions _options = options.Value;
    private readonly object _gate = new();
    private DomainCatalogDto? _catalogCache;
    private DateTime _cachedAtUtc = DateTime.MinValue;

    public IReadOnlyList<DomainTargetOptions> GetTargets()
    {
        var catalog = GetCatalog();
        return catalog.Items
            .Where(item => item.Selected)
            .Select(item => new DomainTargetOptions
            {
                Name = item.Name,
                Url = item.Url
            })
            .ToArray();
    }

    public DomainCatalogDto GetCatalog()
    {
        lock (_gate)
        {
            if (_catalogCache is not null && DateTime.UtcNow - _cachedAtUtc < TimeSpan.FromMinutes(1))
                return _catalogCache;

            _catalogCache = BuildCatalog();
            _cachedAtUtc = DateTime.UtcNow;
            return _catalogCache;
        }
    }

    public void InvalidateCache()
    {
        lock (_gate)
        {
            _catalogCache = null;
            _cachedAtUtc = DateTime.MinValue;
        }
    }

    private DomainCatalogDto BuildCatalog()
    {
        var map = new Dictionary<string, DiscoveredDomainDto>(StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();

        foreach (var domain in _options.Domains)
            Add(map, domain.Name, domain.Url, "config");

        foreach (var local in DiscoverLocalAddresses())
            Add(map, local.Name, local.Url, "local");

        var configPath = Environment.ExpandEnvironmentVariables(_options.IisApplicationHostConfigPath);
        var exists = false;
        var readable = false;

        if (_options.DiscoverIisSites)
        {
            var discovery = iisDiscoverer.Discover();
            exists = discovery.ConfigExists;
            readable = discovery.ConfigReadable;
            configPath = discovery.ConfigPath;
            notes.AddRange(discovery.Notes);

            foreach (var site in discovery.Sites)
                Add(map, site.Name, site.Url, site.Source);
        }

        var selected = selectionStore.GetSelectedUrls();
        foreach (var url in selected)
        {
            if (map.ContainsKey(url))
                continue;

            var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
            Add(map, host, url, "selected");
        }

        var items = map.Values
            .Select(item => new DiscoveredDomainDto
            {
                Name = item.Name,
                Url = item.Url,
                Source = item.Source,
                Selected = selected.Contains(item.Url) || IsConfigured(item.Url)
            })
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DomainCatalogDto
        {
            IisConfigPath = configPath,
            IisConfigExists = exists,
            IisConfigReadable = readable,
            DiscoveryError = notes.Count == 0 ? null : string.Join(" | ", notes),
            DiscoveredCount = items.Length,
            SelectedCount = items.Count(item => item.Selected),
            Items = items
        };
    }

    private IEnumerable<DomainTargetOptions> DiscoverLocalAddresses()
    {
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        foreach (var address in addresses)
        {
            if (!Uri.TryCreate(NormalizeAddress(address), UriKind.Absolute, out var uri))
                continue;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                continue;

            var host = string.Equals(uri.Host, "[::]", StringComparison.Ordinal) || uri.Host == "0.0.0.0" || uri.Host == "+"
                ? "localhost"
                : uri.Host;

            var builder = new UriBuilder(uri) { Host = host, Path = "/" };
            yield return new DomainTargetOptions
            {
                Name = "این برنامه",
                Url = builder.Uri.AbsoluteUri
            };
        }

        var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (string.IsNullOrWhiteSpace(envUrls))
            yield break;

        foreach (var part in envUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(NormalizeAddress(part), UriKind.Absolute, out var uri))
                continue;

            var host = string.Equals(uri.Host, "[::]", StringComparison.Ordinal) || uri.Host == "0.0.0.0" || uri.Host == "+"
                ? "localhost"
                : uri.Host;
            var builder = new UriBuilder(uri) { Host = host, Path = "/" };
            yield return new DomainTargetOptions
            {
                Name = "این برنامه",
                Url = builder.Uri.AbsoluteUri
            };
        }
    }

    private static string NormalizeAddress(string address)
    {
        if (address.StartsWith("http://*", StringComparison.OrdinalIgnoreCase))
            return "http://localhost" + address["http://*".Length..];
        if (address.StartsWith("https://*", StringComparison.OrdinalIgnoreCase))
            return "https://localhost" + address["https://*".Length..];
        if (address.StartsWith("http://+", StringComparison.OrdinalIgnoreCase))
            return "http://localhost" + address["http://+".Length..];
        if (address.StartsWith("https://+", StringComparison.OrdinalIgnoreCase))
            return "https://localhost" + address["https://+".Length..];
        return address;
    }

    private bool IsConfigured(string url)
        => _options.Domains.Any(domain =>
            Uri.TryCreate(domain.Url, UriKind.Absolute, out var configured) &&
            Uri.TryCreate(url, UriKind.Absolute, out var candidate) &&
            string.Equals(configured.AbsoluteUri, candidate.AbsoluteUri, StringComparison.OrdinalIgnoreCase));

    private static void Add(
        IDictionary<string, DiscoveredDomainDto> map,
        string name,
        string url,
        string source)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return;

        if (map.ContainsKey(uri.AbsoluteUri))
            return;

        map[uri.AbsoluteUri] = new DiscoveredDomainDto
        {
            Name = name.Trim(),
            Url = uri.AbsoluteUri,
            Source = source
        };
    }
}

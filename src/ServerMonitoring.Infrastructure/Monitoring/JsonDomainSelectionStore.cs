using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class JsonDomainSelectionStore : IDomainSelectionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _gate = new();
    private readonly string _filePath;
    private HashSet<string>? _cache;

    public JsonDomainSelectionStore(IWebHostEnvironment environment, IOptions<MonitoringOptions> options)
    {
        var directory = Path.IsPathRooted(options.Value.DataDirectory)
            ? options.Value.DataDirectory
            : Path.Combine(environment.ContentRootPath, options.Value.DataDirectory);

        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "selected-domains.json");
    }

    public IReadOnlySet<string> GetSelectedUrls()
    {
        lock (_gate)
        {
            if (_cache is not null)
                return _cache;

            if (!File.Exists(_filePath))
            {
                _cache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return _cache;
            }

            var json = File.ReadAllText(_filePath);
            var document = JsonSerializer.Deserialize<SelectionDocument>(json, JsonOptions);
            _cache = new HashSet<string>(
                document?.SelectedUrls ?? [],
                StringComparer.OrdinalIgnoreCase);
            return _cache;
        }
    }

    public async Task SaveSelectedUrlsAsync(IReadOnlyList<string> urls, CancellationToken cancellationToken)
    {
        var unique = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(url => url, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var payload = JsonSerializer.Serialize(new SelectionDocument { SelectedUrls = unique.ToList() }, JsonOptions);
        await File.WriteAllTextAsync(_filePath, payload, cancellationToken);

        lock (_gate)
            _cache = new HashSet<string>(unique, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SelectionDocument
    {
        public List<string> SelectedUrls { get; set; } = [];
    }
}

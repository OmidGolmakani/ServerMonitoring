using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Infrastructure.Persistence;

public sealed class JsonMonitoringDatabase : IMonitoringDatabase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public JsonMonitoringDatabase(IWebHostEnvironment environment, IOptions<MonitoringOptions> options)
    {
        var directory = Path.IsPathRooted(options.Value.DataDirectory)
            ? options.Value.DataDirectory
            : Path.Combine(environment.ContentRootPath, options.Value.DataDirectory);

        Directory.CreateDirectory(directory);
        FilePath = Path.Combine(directory, "monitoring.json");
        if (!File.Exists(FilePath))
            File.WriteAllText(FilePath, Serialize(new MonitoringDocument()));
    }

    public string FilePath { get; }

    public async Task<IReadOnlyList<RequestTrace>> ReadRequestsAsync(CancellationToken cancellationToken)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Requests ?? [];
    }

    public async Task<IReadOnlyList<SystemSample>> ReadSamplesAsync(CancellationToken cancellationToken)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Samples ?? [];
    }

    public async Task WriteAsync(
        IReadOnlyList<RequestTrace> requests,
        IReadOnlyList<SystemSample> samples,
        CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(Serialize(new MonitoringDocument
        {
            Requests = requests.ToList(),
            Samples = samples.ToList()
        }));

        await using var stream = new FileStream(
            FilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private async Task<MonitoringDocument> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
            return new MonitoringDocument();

        var json = await File.ReadAllTextAsync(FilePath, cancellationToken);
        return JsonSerializer.Deserialize<MonitoringDocument>(json, JsonOptions) ?? new MonitoringDocument();
    }

    private static string Serialize(MonitoringDocument document)
        => JsonSerializer.Serialize(document, JsonOptions);

    private sealed class MonitoringDocument
    {
        public List<RequestTrace> Requests { get; set; } = [];

        public List<SystemSample> Samples { get; set; } = [];
    }
}

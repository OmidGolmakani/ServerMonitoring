namespace ServerMonitoring.Application.Options;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int SampleIntervalMilliseconds { get; set; } = 1000;

    public int FlushIntervalMilliseconds { get; set; } = 1000;

    public int RetentionHours { get; set; } = 24;

    public int LiveRequestCapacity { get; set; } = 20_000;

    public int LiveSampleCapacity { get; set; } = 5_000;

    public int PendingCapacity { get; set; } = 20_000;

    public int PersistenceBatchSize { get; set; } = 500;

    public int DefaultWindowMinutes { get; set; } = 60;

    public int MaxWindowMinutes { get; set; } = 1_440;

    public int DefaultSamplePoints { get; set; } = 240;

    public int RecentRequestLimit { get; set; } = 40;

    public string DataDirectory { get; set; } = "data";

    public int DomainProbeIntervalMilliseconds { get; set; } = 60_000;

    public int DomainProbeTimeoutMilliseconds { get; set; } = 5_000;

    public int DomainProbeMaxConcurrency { get; set; } = 5;

    public int ServerSampleIntervalMilliseconds { get; set; } = 15_000;

    public int LiveServerSampleCapacity { get; set; } = 5_000;

    public int LiveDomainCheckCapacity { get; set; } = 20_000;

    public int TopProcessLimit { get; set; } = 12;

    public string[] ProcessNamesToSample { get; set; } = ["w3wp", "dotnet", "ServerMonitoring.Api"];

    public bool DiscoverIisSites { get; set; } = true;

    public string IisApplicationHostConfigPath { get; set; } =
        @"%windir%\System32\inetsrv\config\applicationHost.config";

    public List<DomainTargetOptions> Domains { get; set; } = [];
}

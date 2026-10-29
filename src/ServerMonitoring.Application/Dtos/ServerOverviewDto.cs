namespace ServerMonitoring.Application.Dtos;

public sealed class ServerOverviewDto
{
    public double CpuPercent { get; init; }

    public long TotalMemoryBytes { get; init; }

    public long AvailableMemoryBytes { get; init; }

    public double MemoryUsedPercent { get; init; }

    public long TotalDiskBytes { get; init; }

    public long AvailableDiskBytes { get; init; }

    public double DiskUsedPercent { get; init; }

    public int ProcessCount { get; init; }

    public int DomainCount { get; init; }

    public int DomainsUp { get; init; }

    public int DomainsDown { get; init; }

    public IReadOnlyList<DiskDto> Disks { get; init; } = [];

    public IReadOnlyList<ProcessUsageDto> TopProcesses { get; init; } = [];
}

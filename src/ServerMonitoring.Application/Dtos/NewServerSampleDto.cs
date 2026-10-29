namespace ServerMonitoring.Application.Dtos;

public sealed class NewServerSampleDto
{
    public DateTime CapturedAtUtc { get; init; }

    public double CpuPercent { get; init; }

    public long TotalMemoryBytes { get; init; }

    public long AvailableMemoryBytes { get; init; }

    public double MemoryUsedPercent { get; init; }

    public long TotalDiskBytes { get; init; }

    public long AvailableDiskBytes { get; init; }

    public int ProcessCount { get; init; }

    public IReadOnlyList<DiskDto> Disks { get; init; } = [];

    public IReadOnlyList<ProcessUsageDto> TopProcesses { get; init; } = [];
}

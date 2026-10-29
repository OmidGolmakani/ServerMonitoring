namespace ServerMonitoring.Domain.Entities;

public sealed class ServerSample
{
    public long Id { get; set; }

    public DateTime CapturedAtUtc { get; set; }

    public double CpuPercent { get; set; }

    public long TotalMemoryBytes { get; set; }

    public long AvailableMemoryBytes { get; set; }

    public double MemoryUsedPercent { get; set; }

    public long TotalDiskBytes { get; set; }

    public long AvailableDiskBytes { get; set; }

    public int ProcessCount { get; set; }

    public List<DiskSample> Disks { get; set; } = [];

    public List<ProcessUsageSample> TopProcesses { get; set; } = [];
}

namespace ServerMonitoring.Domain.Entities;

public sealed class DiskSample
{
    public string Name { get; set; } = string.Empty;

    public long TotalBytes { get; set; }

    public long AvailableBytes { get; set; }
}

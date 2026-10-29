namespace ServerMonitoring.Application.Dtos;

public sealed class DiskDto
{
    public string Name { get; init; } = string.Empty;

    public long TotalBytes { get; init; }

    public long AvailableBytes { get; init; }

    public double UsedPercent { get; init; }
}

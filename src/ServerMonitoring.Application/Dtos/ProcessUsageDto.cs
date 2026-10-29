namespace ServerMonitoring.Application.Dtos;

public sealed class ProcessUsageDto
{
    public string ProcessName { get; init; } = string.Empty;

    public int ProcessId { get; init; }

    public double CpuPercent { get; init; }

    public long WorkingSetBytes { get; init; }
}

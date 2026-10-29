namespace ServerMonitoring.Domain.Entities;

public sealed class ProcessUsageSample
{
    public string ProcessName { get; set; } = string.Empty;

    public int ProcessId { get; set; }

    public double CpuPercent { get; set; }

    public long WorkingSetBytes { get; set; }
}

namespace ServerMonitoring.Application.Dtos;

public sealed class HostProcessInfoDto
{
    public string MachineName { get; init; } = string.Empty;

    public string ProcessName { get; init; } = string.Empty;

    public int ProcessId { get; init; }

    public int ProcessorCount { get; init; }

    public string RuntimeDescription { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public long WorkingSetBytes { get; init; }

    public long PrivateMemoryBytes { get; init; }

    public long GcHeapBytes { get; init; }

    public int ThreadCount { get; init; }
}

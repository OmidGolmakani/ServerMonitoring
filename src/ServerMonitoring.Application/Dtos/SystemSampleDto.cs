namespace ServerMonitoring.Application.Dtos;

public sealed class SystemSampleDto
{
    public DateTime CapturedAtUtc { get; init; }

    public double CpuPercent { get; init; }

    public long WorkingSetBytes { get; init; }

    public long PrivateMemoryBytes { get; init; }

    public long GcHeapBytes { get; init; }

    public long FragmentedBytes { get; init; }

    public double MemoryLoadPercent { get; init; }

    public int ThreadCount { get; init; }

    public int HandleCount { get; init; }

    public int BusyWorkerThreads { get; init; }

    public long PendingWorkItems { get; init; }

    public int ActiveRequestCount { get; init; }

    public double RequestsPerSecond { get; init; }

    public int Gen0Collections { get; init; }

    public int Gen1Collections { get; init; }

    public int Gen2Collections { get; init; }
}

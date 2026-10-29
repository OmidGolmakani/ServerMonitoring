namespace ServerMonitoring.Domain.Entities;

public sealed class SystemSample
{
    public long Id { get; set; }

    public DateTime CapturedAtUtc { get; set; }

    public double CpuPercent { get; set; }

    public long WorkingSetBytes { get; set; }

    public long PrivateMemoryBytes { get; set; }

    public long GcHeapBytes { get; set; }

    public long FragmentedBytes { get; set; }

    public double MemoryLoadPercent { get; set; }

    public int ThreadCount { get; set; }

    public int HandleCount { get; set; }

    public int BusyWorkerThreads { get; set; }

    public long PendingWorkItems { get; set; }

    public int ActiveRequestCount { get; set; }

    public double RequestsPerSecond { get; set; }

    public int Gen0Collections { get; set; }

    public int Gen1Collections { get; set; }

    public int Gen2Collections { get; set; }
}

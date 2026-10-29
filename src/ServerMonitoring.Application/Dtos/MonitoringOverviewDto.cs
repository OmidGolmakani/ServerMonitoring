namespace ServerMonitoring.Application.Dtos;

public sealed class MonitoringOverviewDto
{
    public DateTime GeneratedAtUtc { get; init; }

    public string MachineName { get; init; } = string.Empty;

    public string ProcessName { get; init; } = string.Empty;

    public int ProcessId { get; init; }

    public int ProcessorCount { get; init; }

    public string RuntimeDescription { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public double UptimeSeconds { get; init; }

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

    public int ActiveRequests { get; init; }

    public double RequestsPerSecond { get; init; }

    public double WindowRequestsPerSecond { get; init; }

    public long TotalRequests { get; init; }

    public long TotalErrors { get; init; }

    public double ErrorRatePercent { get; init; }

    public double AverageElapsedMilliseconds { get; init; }

    public double P95ElapsedMilliseconds { get; init; }

    public double TotalCpuMilliseconds { get; init; }

    public long TotalAllocatedBytes { get; init; }

    public long TotalResponseBytes { get; init; }

    public int Gen0Collections { get; init; }

    public int Gen1Collections { get; init; }

    public int Gen2Collections { get; init; }

    public long DroppedObservations { get; init; }

    public int WindowMinutes { get; init; }

    public double WindowSeconds { get; init; }

    public string DatabasePath { get; init; } = string.Empty;
}

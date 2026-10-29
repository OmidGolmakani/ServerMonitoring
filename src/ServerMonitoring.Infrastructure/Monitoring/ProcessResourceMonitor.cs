using System.Diagnostics;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Infrastructure.Monitoring;

// سهم هر درخواست از پردازنده و تخصیص حافظه: دلتای کل فرآیند از تیک قبلی،
// تقسیم برابر بین درخواست‌های در حال اجرا. پایان درخواست هم یک تیک می‌زند
// تا درخواست‌های کوتاه‌تر از فاصله نمونه‌برداری بی‌سهم نمانند.
public sealed class ProcessResourceMonitor : IRequestInstrumentation, ISystemSampleProbe
{
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly RequestExecutionRegistry _registry = new();
    private readonly Lock _attributionGate = new();
    private readonly Lock _sampleGate = new();
    private TimeSpan _lastAttributedCpu;
    private long _lastAttributedAllocations;
    private TimeSpan _lastSampleCpu;
    private long _lastWallTimestamp;
    private int _lastGen0;
    private int _lastGen1;
    private int _lastGen2;
    private long _completedSinceSample;

    public ProcessResourceMonitor()
    {
        _process.Refresh();
        var cpu = _process.TotalProcessorTime;
        _lastAttributedCpu = cpu;
        _lastSampleCpu = cpu;
        _lastAttributedAllocations = GC.GetTotalAllocatedBytes(precise: false);
        _lastWallTimestamp = Stopwatch.GetTimestamp();
        _lastGen0 = GC.CollectionCount(0);
        _lastGen1 = GC.CollectionCount(1);
        _lastGen2 = GC.CollectionCount(2);
    }

    public int ActiveRequests => _registry.Count;

    public long Begin() => _registry.Begin();

    public RequestUsageDto Complete(long executionId)
    {
        AttributePending();
        Interlocked.Increment(ref _completedSinceSample);
        return _registry.Take(executionId);
    }

    public void AttributePending()
    {
        lock (_attributionGate)
        {
            _process.Refresh();
            var cpu = _process.TotalProcessorTime;
            var allocated = GC.GetTotalAllocatedBytes(precise: false);
            var cpuDelta = Math.Max(0d, (cpu - _lastAttributedCpu).TotalMilliseconds);
            var allocationDelta = Math.Max(0L, allocated - _lastAttributedAllocations);
            _registry.AttributeAll(cpuDelta, allocationDelta);
            _lastAttributedCpu = cpu;
            _lastAttributedAllocations = allocated;
        }
    }

    public NewSystemSampleDto Capture()
    {
        AttributePending();
        lock (_sampleGate)
        {
            _process.Refresh();
            var cpu = _process.TotalProcessorTime;
            var wall = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(_lastWallTimestamp, wall);
            var intervalSeconds = Math.Max(0.001, elapsed.TotalSeconds);
            var cpuMilliseconds = Math.Max(0d, (cpu - _lastSampleCpu).TotalMilliseconds);
            var cpuPercent = cpuMilliseconds / (intervalSeconds * Environment.ProcessorCount * 1000d) * 100d;
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var completed = Interlocked.Exchange(ref _completedSinceSample, 0);
            var memory = GC.GetGCMemoryInfo();
            var available = memory.TotalAvailableMemoryBytes;
            var memoryLoad = available <= 0 ? 0d : memory.MemoryLoadBytes * 100d / available;

            ThreadPool.GetAvailableThreads(out var availableWorkers, out _);
            ThreadPool.GetMaxThreads(out var maxWorkers, out _);

            _lastSampleCpu = cpu;
            _lastWallTimestamp = wall;

            var sample = new NewSystemSampleDto
            {
                CapturedAtUtc = DateTime.UtcNow,
                CpuPercent = Math.Round(cpuPercent, 2, MidpointRounding.AwayFromZero),
                WorkingSetBytes = _process.WorkingSet64,
                PrivateMemoryBytes = _process.PrivateMemorySize64,
                GcHeapBytes = GC.GetTotalMemory(forceFullCollection: false),
                FragmentedBytes = memory.FragmentedBytes,
                MemoryLoadPercent = Math.Round(memoryLoad, 2, MidpointRounding.AwayFromZero),
                ThreadCount = _process.Threads.Count,
                HandleCount = OperatingSystem.IsWindows() ? _process.HandleCount : 0,
                BusyWorkerThreads = Math.Max(0, maxWorkers - availableWorkers),
                PendingWorkItems = ThreadPool.PendingWorkItemCount,
                ActiveRequestCount = _registry.Count,
                RequestsPerSecond = Math.Round(completed / intervalSeconds, 2, MidpointRounding.AwayFromZero),
                Gen0Collections = Math.Max(0, gen0 - _lastGen0),
                Gen1Collections = Math.Max(0, gen1 - _lastGen1),
                Gen2Collections = Math.Max(0, gen2 - _lastGen2)
            };

            _lastGen0 = gen0;
            _lastGen1 = gen1;
            _lastGen2 = gen2;
            return sample;
        }
    }

    private sealed class RequestExecutionRegistry
    {
        private long _nextId;
        private readonly Dictionary<long, InFlightExecution> _active = new();
        private readonly Lock _gate = new();

        public int Count
        {
            get
            {
                lock (_gate)
                    return _active.Count;
            }
        }

        public long Begin()
        {
            var id = Interlocked.Increment(ref _nextId);
            lock (_gate)
                _active.Add(id, new InFlightExecution());

            return id;
        }

        public void AttributeAll(double cpuMilliseconds, long allocatedBytes)
        {
            lock (_gate)
            {
                var count = _active.Count;
                if (count == 0 || (cpuMilliseconds <= 0 && allocatedBytes <= 0))
                    return;

                var cpuShare = cpuMilliseconds / count;
                var allocationShare = allocatedBytes / count;
                foreach (var execution in _active.Values)
                    execution.Add(cpuShare, allocationShare);
            }
        }

        public RequestUsageDto Take(long id)
        {
            lock (_gate)
            {
                if (!_active.Remove(id, out var execution))
                    return new RequestUsageDto();

                return new RequestUsageDto
                {
                    CpuMilliseconds = execution.CpuMilliseconds,
                    AllocatedBytes = execution.AllocatedBytes
                };
            }
        }
    }

    private sealed class InFlightExecution
    {
        public double CpuMilliseconds { get; private set; }

        public long AllocatedBytes { get; private set; }

        public void Add(double cpuMilliseconds, long allocatedBytes)
        {
            CpuMilliseconds += cpuMilliseconds;
            AllocatedBytes += allocatedBytes;
        }
    }
}

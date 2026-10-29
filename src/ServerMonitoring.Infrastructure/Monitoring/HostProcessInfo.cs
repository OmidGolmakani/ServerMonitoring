using System.Diagnostics;
using System.Runtime.InteropServices;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class HostProcessInfo : IHostProcessInfo
{
    private readonly DateTime _startedAtUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    public HostProcessInfoDto GetCurrent()
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();

        return new HostProcessInfoDto
        {
            MachineName = Environment.MachineName,
            ProcessName = process.ProcessName,
            ProcessId = Environment.ProcessId,
            ProcessorCount = Environment.ProcessorCount,
            RuntimeDescription = RuntimeInformation.FrameworkDescription,
            StartedAtUtc = _startedAtUtc,
            WorkingSetBytes = process.WorkingSet64,
            PrivateMemoryBytes = process.PrivateMemorySize64,
            GcHeapBytes = GC.GetTotalMemory(forceFullCollection: false),
            ThreadCount = process.Threads.Count
        };
    }
}

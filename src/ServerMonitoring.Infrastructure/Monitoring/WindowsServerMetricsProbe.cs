using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class WindowsServerMetricsProbe(IOptions<MonitoringOptions> options) : IServerMetricsProbe
{
    private readonly MonitoringOptions _options = options.Value;
    private readonly object _gate = new();
    private bool _hasBaseline;
    private ulong _idle;
    private ulong _kernel;
    private ulong _user;
    private readonly Dictionary<int, TimeSpan> _processCpu = new();

    public NewServerSampleDto Capture()
    {
        var cpu = CaptureCpuPercent();
        var memory = CaptureMemory();
        var disks = CaptureDisks();
        var processSnapshot = CaptureWatchedProcesses();
        var totalDisk = disks.Sum(disk => disk.TotalBytes);
        var availableDisk = disks.Sum(disk => disk.AvailableBytes);

        return new NewServerSampleDto
        {
            CapturedAtUtc = DateTime.UtcNow,
            CpuPercent = cpu,
            TotalMemoryBytes = memory.Total,
            AvailableMemoryBytes = memory.Available,
            MemoryUsedPercent = memory.Total <= 0
                ? 0
                : Math.Round((memory.Total - memory.Available) * 100d / memory.Total, 2),
            TotalDiskBytes = totalDisk,
            AvailableDiskBytes = availableDisk,
            ProcessCount = processSnapshot.Count,
            Disks = disks,
            TopProcesses = processSnapshot.Top
        };
    }

    private double CaptureCpuPercent()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            return 0;

        var idle = ToUInt64(idleTime);
        var kernel = ToUInt64(kernelTime);
        var user = ToUInt64(userTime);

        lock (_gate)
        {
            if (!_hasBaseline)
            {
                _idle = idle;
                _kernel = kernel;
                _user = user;
                _hasBaseline = true;
                return 0;
            }

            var idleDelta = idle - _idle;
            var kernelDelta = kernel - _kernel;
            var userDelta = user - _user;
            _idle = idle;
            _kernel = kernel;
            _user = user;

            var total = kernelDelta + userDelta;
            if (total == 0)
                return 0;

            var busy = total - idleDelta;
            return Math.Round(Math.Clamp(busy * 100d / total, 0, 100), 2);
        }
    }

    private static (long Total, long Available) CaptureMemory()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
            return (0, 0);

        return ((long)status.TotalPhys, (long)status.AvailPhys);
    }

    private static IReadOnlyList<DiskDto> CaptureDisks()
    {
        return DriveInfo.GetDrives()
            .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady)
            .Select(drive => new DiskDto
            {
                Name = drive.Name,
                TotalBytes = drive.TotalSize,
                AvailableBytes = drive.AvailableFreeSpace,
                UsedPercent = drive.TotalSize <= 0
                    ? 0
                    : Math.Round((drive.TotalSize - drive.AvailableFreeSpace) * 100d / drive.TotalSize, 2)
            })
            .OrderBy(disk => disk.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private (int Count, IReadOnlyList<ProcessUsageDto> Top) CaptureWatchedProcesses()
    {
        var names = _options.ProcessNamesToSample is { Length: > 0 }
            ? _options.ProcessNamesToSample
            : ["w3wp", "dotnet"];

        var snapshots = new List<ProcessUsageDto>();
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(name);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var process in processes)
            {
                var usage = TryReadProcess(process);
                if (usage is not null)
                    snapshots.Add(usage);
            }
        }

        return (
            snapshots.Count,
            snapshots
                .OrderByDescending(item => item.WorkingSetBytes)
                .Take(_options.TopProcessLimit)
                .ToArray());
    }

    private ProcessUsageDto? TryReadProcess(Process process)
    {
        try
        {
            process.Refresh();
            var cpu = process.TotalProcessorTime;
            var workingSet = process.WorkingSet64;
            var name = process.ProcessName;
            var id = process.Id;
            double cpuPercent = 0;

            lock (_gate)
            {
                if (_processCpu.TryGetValue(id, out var previous))
                {
                    var delta = (cpu - previous).TotalMilliseconds;
                    var interval = Math.Max(1, _options.ServerSampleIntervalMilliseconds);
                    cpuPercent = Math.Round(
                        Math.Clamp(delta / (interval * Environment.ProcessorCount) * 100d, 0, 100),
                        2);
                }

                _processCpu[id] = cpu;
            }

            return new ProcessUsageDto
            {
                ProcessName = name,
                ProcessId = id,
                CpuPercent = cpuPercent,
                WorkingSetBytes = workingSet
            };
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            process.Dispose();
        }
    }

    private static ulong ToUInt64(FileTime time)
        => ((ulong)time.HighDateTime << 32) | time.LowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}

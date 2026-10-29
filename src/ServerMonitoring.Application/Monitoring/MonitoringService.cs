using System.Globalization;
using AutoMapper;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Monitoring;

public sealed class MonitoringService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IOptions<MonitoringOptions> options,
    IRequestInstrumentation instrumentation,
    IHostProcessInfo hostProcessInfo,
    IHtmlReportRenderer reportRenderer,
    IMonitoringDatabase monitoringDatabase) : IMonitoringService
{
    private static readonly CultureInfo PersianCulture = CultureInfo.GetCultureInfo("fa-IR");

    private readonly MonitoringOptions _options = options.Value;

    public void RecordRequest(NewRequestObservationDto observation)
    {
        if (string.IsNullOrWhiteSpace(observation.Route))
            throw new ValidationException("مسیر درخواست خالی است.");

        var trace = mapper.Map<RequestTrace>(observation);
        unitOfWork.RequestTraces.Stage(trace);
    }

    public void RecordSample(NewSystemSampleDto sample)
    {
        var entity = mapper.Map<SystemSample>(sample);
        unitOfWork.SystemSamples.Stage(entity);
    }

    public async Task<MonitoringOverviewDto> GetOverviewAsync(int? windowMinutes, CancellationToken cancellationToken)
    {
        var dashboard = await BuildAsync(windowMinutes, cancellationToken);
        return dashboard.Overview;
    }

    public async Task<IReadOnlyList<RouteMetricDto>> GetRoutesAsync(int? windowMinutes, CancellationToken cancellationToken)
    {
        var dashboard = await BuildAsync(windowMinutes, cancellationToken);
        return dashboard.Routes;
    }

    public async Task<IReadOnlyList<SystemSampleDto>> GetSamplesAsync(int? windowMinutes, CancellationToken cancellationToken)
    {
        var dashboard = await BuildAsync(windowMinutes, cancellationToken);
        return dashboard.Samples;
    }

    public Task<DashboardDto> GetDashboardAsync(int? windowMinutes, CancellationToken cancellationToken)
        => BuildAsync(windowMinutes, cancellationToken);

    public async Task<string> BuildHtmlReportAsync(int? windowMinutes, CancellationToken cancellationToken)
    {
        var dashboard = await BuildAsync(windowMinutes, cancellationToken);
        return reportRenderer.Render(dashboard);
    }

    private async Task<DashboardDto> BuildAsync(int? windowMinutes, CancellationToken cancellationToken)
    {
        var window = NormalizeWindow(windowMinutes);
        var fromUtc = DateTime.UtcNow.AddMinutes(-window);
        var process = hostProcessInfo.GetCurrent();
        var windowSeconds = ResolveWindowSeconds(fromUtc, process.StartedAtUtc);

        var traces = await unitOfWork.RequestTraces.GetSinceAsync(fromUtc, cancellationToken);
        var samples = await unitOfWork.SystemSamples.GetSinceAsync(fromUtc, cancellationToken);
        var routes = RouteMetricCalculator.Calculate(traces, windowSeconds);
        var visibleSamples = SampleDownsampler.Downsample(samples, _options.DefaultSamplePoints);
        var latest = samples.Count == 0 ? null : samples[^1];
        var recent = traces
            .OrderByDescending(trace => trace.OccurredAtUtc)
            .Take(_options.RecentRequestLimit)
            .ToArray();

        var totalErrors = traces.Count(trace => trace.StatusCode >= 400);
        var elapsed = traces.Select(trace => trace.ElapsedMilliseconds).ToArray();

        var overview = new MonitoringOverviewDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            MachineName = process.MachineName,
            ProcessName = process.ProcessName,
            ProcessId = process.ProcessId,
            ProcessorCount = process.ProcessorCount,
            RuntimeDescription = process.RuntimeDescription,
            StartedAtUtc = process.StartedAtUtc,
            UptimeSeconds = Math.Max(0, (DateTime.UtcNow - process.StartedAtUtc).TotalSeconds),
            CpuPercent = latest?.CpuPercent ?? 0,
            WorkingSetBytes = latest?.WorkingSetBytes ?? process.WorkingSetBytes,
            PrivateMemoryBytes = latest?.PrivateMemoryBytes ?? process.PrivateMemoryBytes,
            GcHeapBytes = latest?.GcHeapBytes ?? process.GcHeapBytes,
            FragmentedBytes = latest?.FragmentedBytes ?? 0,
            MemoryLoadPercent = latest?.MemoryLoadPercent ?? 0,
            ThreadCount = latest?.ThreadCount ?? process.ThreadCount,
            HandleCount = latest?.HandleCount ?? 0,
            BusyWorkerThreads = latest?.BusyWorkerThreads ?? 0,
            PendingWorkItems = latest?.PendingWorkItems ?? 0,
            ActiveRequests = instrumentation.ActiveRequests,
            RequestsPerSecond = latest?.RequestsPerSecond ?? 0,
            WindowRequestsPerSecond = Round(traces.Count / windowSeconds),
            TotalRequests = traces.Count,
            TotalErrors = totalErrors,
            ErrorRatePercent = traces.Count == 0 ? 0 : Round(totalErrors * 100d / traces.Count),
            AverageElapsedMilliseconds = traces.Count == 0 ? 0 : Round(traces.Average(trace => trace.ElapsedMilliseconds)),
            P95ElapsedMilliseconds = Round(RouteMetricCalculator.Percentile(elapsed, 0.95)),
            TotalCpuMilliseconds = Round(traces.Sum(trace => trace.CpuMilliseconds)),
            TotalAllocatedBytes = traces.Sum(trace => trace.AllocatedBytes),
            TotalResponseBytes = traces.Sum(trace => trace.ResponseBytes),
            Gen0Collections = latest?.Gen0Collections ?? 0,
            Gen1Collections = latest?.Gen1Collections ?? 0,
            Gen2Collections = latest?.Gen2Collections ?? 0,
            DroppedObservations = unitOfWork.RequestTraces.DroppedObservations,
            WindowMinutes = window,
            WindowSeconds = Round(windowSeconds),
            DatabasePath = monitoringDatabase.FilePath
        };

        return new DashboardDto
        {
            Overview = overview,
            Routes = routes,
            Samples = mapper.Map<List<SystemSampleDto>>(visibleSamples),
            StatusShares = RouteMetricCalculator.CalculateStatusShares(traces),
            RecentRequests = mapper.Map<List<RecentRequestDto>>(recent),
            Highlights = BuildHighlights(routes)
        };
    }

    private int NormalizeWindow(int? windowMinutes)
    {
        var window = windowMinutes ?? _options.DefaultWindowMinutes;
        if (window < 1 || window > _options.MaxWindowMinutes)
        {
            throw new ValidationException(
                "بازه زمانی گزارش نامعتبر است.",
                new Dictionary<string, string[]>
                {
                    ["windowMinutes"] =
                    [
                        $"بازه باید بین ۱ و {_options.MaxWindowMinutes.ToString(CultureInfo.InvariantCulture)} دقیقه باشد."
                    ]
                });
        }

        return window;
    }

    private static double ResolveWindowSeconds(DateTime fromUtc, DateTime startedAtUtc)
    {
        var start = startedAtUtc > fromUtc ? startedAtUtc : fromUtc;
        return Math.Max(1d, (DateTime.UtcNow - start).TotalSeconds);
    }

    private static IReadOnlyList<MonitoringHighlightDto> BuildHighlights(IReadOnlyList<RouteMetricDto> routes)
    {
        if (routes.Count == 0)
            return [];

        var highlights = new List<MonitoringHighlightDto>();
        var slowest = routes.MaxBy(route => route.P95ElapsedMilliseconds)!;
        var busiest = routes.MaxBy(route => route.RequestCount)!;

        highlights.Add(new MonitoringHighlightDto
        {
            Title = "کندترین مسیر",
            Value = FormatRoute(slowest),
            Detail = $"صدک ۹۵ برابر {FormatMilliseconds(slowest.P95ElapsedMilliseconds)}"
        });

        highlights.Add(new MonitoringHighlightDto
        {
            Title = "پررفت‌وآمدترین مسیر",
            Value = FormatRoute(busiest),
            Detail = $"{busiest.RequestCount.ToString("N0", PersianCulture)} درخواست"
        });

        var heaviestCpu = routes.MaxBy(route => route.TotalCpuMilliseconds)!;
        if (heaviestCpu.TotalCpuMilliseconds > 0)
        {
            highlights.Add(new MonitoringHighlightDto
            {
                Title = "بیشترین سهم پردازنده",
                Value = FormatRoute(heaviestCpu),
                Detail = $"{heaviestCpu.CpuSharePercent.ToString("0.0", PersianCulture)}٪ از پردازندهٔ نسبت‌داده‌شده"
            });
        }

        var heaviestMemory = routes.MaxBy(route => route.TotalAllocatedBytes)!;
        if (heaviestMemory.TotalAllocatedBytes > 0)
        {
            highlights.Add(new MonitoringHighlightDto
            {
                Title = "بیشترین تخصیص حافظه",
                Value = FormatRoute(heaviestMemory),
                Detail = FormatBytes(heaviestMemory.TotalAllocatedBytes)
            });
        }

        var mostErrors = routes.MaxBy(route => route.ErrorCount)!;
        if (mostErrors.ErrorCount > 0)
        {
            highlights.Add(new MonitoringHighlightDto
            {
                Title = "بیشترین خطا",
                Value = FormatRoute(mostErrors),
                Detail = $"{mostErrors.ErrorCount.ToString("N0", PersianCulture)} پاسخ ناموفق"
            });
        }

        return highlights;
    }

    private static string FormatRoute(RouteMetricDto route) => $"{route.HttpMethod} {route.Route}";

    private static string FormatMilliseconds(double milliseconds)
    {
        if (milliseconds < 1000)
            return $"{milliseconds.ToString("0.0", PersianCulture)} میلی‌ثانیه";

        return $"{(milliseconds / 1000d).ToString("0.00", PersianCulture)} ثانیه";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["بایت", "کیلوبایت", "مگابایت", "گیگابایت"];
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var format = value >= 10 || unit == 0 ? "0" : "0.0";
        return $"{value.ToString(format, PersianCulture)} {units[unit]}";
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

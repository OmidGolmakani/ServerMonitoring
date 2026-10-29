using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Monitoring;

public static class RouteMetricCalculator
{
    public static IReadOnlyList<RouteMetricDto> Calculate(IReadOnlyList<RequestTrace> traces, double windowSeconds)
    {
        if (traces.Count == 0)
            return [];

        var seconds = Math.Max(1d, windowSeconds);
        var groups = traces
            .GroupBy(trace => (trace.HttpMethod, trace.Route))
            .Select(group =>
            {
                var elapsed = group.Select(trace => trace.ElapsedMilliseconds).ToArray();
                var count = group.LongCount();
                var totalCpu = group.Sum(trace => trace.CpuMilliseconds);
                var totalAllocated = group.Sum(trace => trace.AllocatedBytes);

                return new RouteMetricDto
                {
                    HttpMethod = group.Key.HttpMethod,
                    Route = group.Key.Route,
                    RequestCount = count,
                    ErrorCount = group.LongCount(trace => trace.StatusCode >= 400),
                    AverageElapsedMilliseconds = Round(group.Average(trace => trace.ElapsedMilliseconds)),
                    P95ElapsedMilliseconds = Round(Percentile(elapsed, 0.95)),
                    MaxElapsedMilliseconds = Round(group.Max(trace => trace.ElapsedMilliseconds)),
                    TotalCpuMilliseconds = Round(totalCpu),
                    AverageCpuMilliseconds = Round(totalCpu / count),
                    TotalAllocatedBytes = totalAllocated,
                    TotalResponseBytes = group.Sum(trace => trace.ResponseBytes),
                    RequestsPerSecond = Round(count / seconds)
                };
            })
            .ToList();

        var cpuTotal = groups.Sum(route => route.TotalCpuMilliseconds);
        var allocationTotal = groups.Sum(route => route.TotalAllocatedBytes);

        return groups
            .Select(route => new RouteMetricDto
            {
                HttpMethod = route.HttpMethod,
                Route = route.Route,
                RequestCount = route.RequestCount,
                ErrorCount = route.ErrorCount,
                AverageElapsedMilliseconds = route.AverageElapsedMilliseconds,
                P95ElapsedMilliseconds = route.P95ElapsedMilliseconds,
                MaxElapsedMilliseconds = route.MaxElapsedMilliseconds,
                TotalCpuMilliseconds = route.TotalCpuMilliseconds,
                AverageCpuMilliseconds = route.AverageCpuMilliseconds,
                CpuSharePercent = cpuTotal <= 0 ? 0 : Round(route.TotalCpuMilliseconds * 100d / cpuTotal),
                TotalAllocatedBytes = route.TotalAllocatedBytes,
                AllocationSharePercent = allocationTotal <= 0 ? 0 : Round(route.TotalAllocatedBytes * 100d / allocationTotal),
                TotalResponseBytes = route.TotalResponseBytes,
                RequestsPerSecond = route.RequestsPerSecond
            })
            .OrderByDescending(route => route.TotalCpuMilliseconds)
            .ThenByDescending(route => route.RequestCount)
            .ToArray();
    }

    public static IReadOnlyList<StatusShareDto> CalculateStatusShares(IReadOnlyList<RequestTrace> traces)
    {
        if (traces.Count == 0)
            return [];

        var total = traces.Count;
        return traces
            .GroupBy(trace => trace.StatusCode)
            .Select(group => new StatusShareDto
            {
                StatusCode = group.Key,
                Count = group.LongCount(),
                Percent = Round(group.LongCount() * 100d / total)
            })
            .OrderByDescending(share => share.Count)
            .ToArray();
    }

    public static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
            return 0;

        if (percentile is <= 0 or > 1)
            throw new ValidationException("صدک باید بزرگ‌تر از صفر و حداکثر یک باشد.");

        var sorted = values.OrderBy(value => value).ToArray();
        var rank = (int)Math.Ceiling(percentile * sorted.Length);
        rank = Math.Clamp(rank, 1, sorted.Length);
        return sorted[rank - 1];
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

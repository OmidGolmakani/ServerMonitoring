using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Monitoring;

public static class DomainStatusCalculator
{
    public static IReadOnlyList<DomainStatusDto> Calculate(
        IReadOnlyList<DomainTargetOptions> targets,
        IReadOnlyList<DomainCheck> checks)
    {
        var grouped = checks
            .GroupBy(check => check.Url, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        return targets
            .Select(target =>
            {
                grouped.TryGetValue(target.Url, out var history);
                history ??= [];
                var latest = history.OrderByDescending(item => item.CheckedAtUtc).FirstOrDefault();
                var upCount = history.Count(item => item.IsUp);
                return new DomainStatusDto
                {
                    Name = target.Name,
                    Url = target.Url,
                    IsUp = latest?.IsUp ?? false,
                    StatusCode = latest?.StatusCode,
                    ElapsedMilliseconds = latest?.ElapsedMilliseconds ?? 0,
                    LastCheckedAtUtc = latest?.CheckedAtUtc,
                    Error = latest?.Error,
                    CheckCount = history.Length,
                    UpPercent = history.Length == 0 ? 0 : Math.Round(upCount * 100d / history.Length, 2)
                };
            })
            .OrderBy(status => status.IsUp)
            .ThenBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

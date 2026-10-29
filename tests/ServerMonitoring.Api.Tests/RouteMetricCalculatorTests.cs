using ServerMonitoring.Application.Monitoring;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Api.Tests;

public class RouteMetricCalculatorTests
{
    /// <summary>
    /// سناریو: هیچ ردی از درخواست نرسیده است.
    /// انتظار: هر دو خروجی خالی باشند و تقسیم بر صفر رخ ندهد.
    /// </summary>
    [Fact(DisplayName = "بدون درخواست، متریک مسیر و سهم وضعیت خالی است")]
    public void Empty_traces_produce_empty_metrics()
    {
        var routes = RouteMetricCalculator.Calculate([], windowSeconds: 60);
        var shares = RouteMetricCalculator.CalculateStatusShares([]);

        Assert.Empty(routes);
        Assert.Empty(shares);
    }

    /// <summary>
    /// سناریو: صد مقدار ۱ تا ۱۰۰. صدک ۹۵ با روش nearest-rank برابر ceil(0.95 * 100) = 95 است.
    /// انتظار: مقدار ۹۵ برگردد.
    /// </summary>
    [Fact(DisplayName = "صدک ۹۵ برای مقادیر ۱ تا ۱۰۰ برابر ۹۵ است")]
    public void Percentile_uses_nearest_rank()
    {
        var values = Enumerable.Range(1, 100).Select(value => (double)value).ToArray();

        var percentile = RouteMetricCalculator.Percentile(values, 0.95);

        Assert.Equal(95, percentile);
    }

    /// <summary>
    /// سناریو: دو متد روی یک مسیر و یک مسیر دیگر.
    /// انتظار: گروه‌بندی با روش و مسیر جدا باشد، میانگین و حداکثر و نرخ درست حساب شود.
    /// </summary>
    [Fact(DisplayName = "متریک‌ها بر اساس روش و مسیر جدا و میانگین‌ها درست است")]
    public void Groups_by_method_and_route_and_computes_averages()
    {
        var now = DateTime.UtcNow;
        RequestTrace[] traces =
        [
            Trace("GET", "/api/items", 200, 100, 10, 1000, now),
            Trace("GET", "/api/items", 500, 300, 30, 3000, now),
            Trace("POST", "/api/items", 201, 50, 5, 500, now)
        ];

        var routes = RouteMetricCalculator.Calculate(traces, windowSeconds: 10);
        var get = Assert.Single(routes, route => route.HttpMethod == "GET");
        var post = Assert.Single(routes, route => route.HttpMethod == "POST");

        Assert.Equal(2, get.RequestCount);
        Assert.Equal(1, get.ErrorCount);
        Assert.Equal(200, get.AverageElapsedMilliseconds);
        Assert.Equal(300, get.MaxElapsedMilliseconds);
        Assert.Equal(40, get.TotalCpuMilliseconds);
        Assert.Equal(0.2, get.RequestsPerSecond);
        Assert.Equal(1, post.RequestCount);
        Assert.Equal(0, post.ErrorCount);
    }

    /// <summary>
    /// سناریو: سه مسیر با پردازنده ۵۰ و ۳۰ و ۲۰ و حافظه ۷۰ و ۳۰ و ۰.
    /// انتظار: سهم‌ها همان درصدها باشند و پردازنده صفر تقسیم بر صفر نسازد.
    /// </summary>
    [Fact(DisplayName = "سهم پردازنده و حافظه بین مسیرها تقسیم می‌شود")]
    public void Shares_cpu_and_allocation_without_dividing_by_zero()
    {
        var now = DateTime.UtcNow;
        RequestTrace[] traces =
        [
            Trace("GET", "/a", 200, 1, 50, 70, now),
            Trace("GET", "/b", 200, 1, 30, 30, now),
            Trace("GET", "/c", 204, 1, 0, 0, now)
        ];

        var routes = RouteMetricCalculator.Calculate(traces, windowSeconds: 60);

        Assert.Equal(62.5, Assert.Single(routes, route => route.Route == "/a").CpuSharePercent);
        Assert.Equal(37.5, Assert.Single(routes, route => route.Route == "/b").CpuSharePercent);
        Assert.Equal(0, Assert.Single(routes, route => route.Route == "/c").CpuSharePercent);
        Assert.Equal(70, Assert.Single(routes, route => route.Route == "/a").AllocationSharePercent);
    }

    /// <summary>
    /// سناریو: کدهای ۲۰۰، ۳۹۹، ۴۰۰ و ۵۰۰.
    /// انتظار: فقط ۴۰۰ و ۵۰۰ خطا باشند و سهم وضعیت‌ها جمعشان ۱۰۰ شود.
    /// </summary>
    [Fact(DisplayName = "فقط وضعیت ۴۰۰ به بالا خطا حساب می‌شود")]
    public void Counts_only_status_codes_from_400_as_errors()
    {
        var now = DateTime.UtcNow;
        RequestTrace[] traces =
        [
            Trace("GET", "/ok", 200, 1, 0, 0, now),
            Trace("GET", "/edge", 399, 1, 0, 0, now),
            Trace("GET", "/bad", 400, 1, 0, 0, now),
            Trace("GET", "/fail", 500, 1, 0, 0, now)
        ];

        var routes = RouteMetricCalculator.Calculate(traces, 60);
        var shares = RouteMetricCalculator.CalculateStatusShares(traces);

        Assert.Equal(0, Assert.Single(routes, route => route.Route == "/edge").ErrorCount);
        Assert.Equal(1, Assert.Single(routes, route => route.Route == "/bad").ErrorCount);
        Assert.Equal(100, shares.Sum(share => share.Percent), precision: 2);
    }

    /// <summary>
    /// سناریو: پنج نقطه و سقف سه نقطه.
    /// انتظار: سه نقطه برگردد و اول و آخر حفظ شوند.
    /// </summary>
    [Fact(DisplayName = "نمونه‌کاهی ابتدا و انتهای سری را نگه می‌دارد")]
    public void Downsample_keeps_ends_and_requested_count()
    {
        int[] source = [1, 2, 3, 4, 5];

        var sampled = SampleDownsampler.Downsample(source, 3);

        Assert.Equal([1, 3, 5], sampled);
    }

    /// <summary>
    /// سناریو: تعداد نقطه نامعتبر.
    /// انتظار: ValidationException پرتاب شود تا میان‌افزار آن را به ۴۰۰ تبدیل کند.
    /// </summary>
    [Fact(DisplayName = "نمونه‌کاهی با تعداد نامعتبر اعتبارسنجی پرتاب می‌کند")]
    public void Downsample_rejects_non_positive_point_count()
    {
        var exception = Assert.Throws<ValidationException>(() => SampleDownsampler.Downsample(new[] { 1 }, 0));

        Assert.Equal(400, exception.StatusCode);
    }

    /// <summary>
    /// سناریو: تنظیم پیش‌فرض و یک بازه نمونه‌برداری کوتاه‌تر از حد.
    /// انتظار: پیش‌فرض معتبر باشد و مقدار کوتاه رد شود.
    /// </summary>
    [Fact(DisplayName = "اعتبارسنج تنظیمات، بازه نمونه‌برداری کوتاه را رد می‌کند")]
    public void Options_validator_rejects_too_small_sample_interval()
    {
        var validator = new MonitoringOptionsValidator();

        Assert.True(validator.Validate(null, new MonitoringOptions()).Succeeded);
        Assert.False(validator.Validate(null, new MonitoringOptions { SampleIntervalMilliseconds = 10 }).Succeeded);
        Assert.False(validator.Validate(null, new MonitoringOptions { DataDirectory = " " }).Succeeded);
    }

    private static RequestTrace Trace(
        string method,
        string route,
        int status,
        double elapsed,
        double cpu,
        long allocated,
        DateTime occurred)
        => new()
        {
            HttpMethod = method,
            Route = route,
            StatusCode = status,
            ElapsedMilliseconds = elapsed,
            CpuMilliseconds = cpu,
            AllocatedBytes = allocated,
            OccurredAtUtc = occurred
        };
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ServerMonitoring.Application.Common;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Entities;
using ServerMonitoring.Infrastructure.Persistence;

namespace ServerMonitoring.Api.Tests;

public class MonitoringApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;

    public MonitoringApiFactory()
        : this("Development")
    {
    }

    protected MonitoringApiFactory(string environment)
    {
        _environment = environment;
        DataDirectory = Path.Combine(Path.GetTempPath(), "monitoring-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DataDirectory);
    }

    public string DataDirectory { get; }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting("Monitoring:DataDirectory", DataDirectory);
        builder.UseSetting("Monitoring:SampleIntervalMilliseconds", "400");
        builder.UseSetting("Monitoring:FlushIntervalMilliseconds", "60000");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(DataDirectory))
            Directory.Delete(DataDirectory, recursive: true);
    }
}

public sealed class ProductionMonitoringApiFactory : MonitoringApiFactory
{
    public ProductionMonitoringApiFactory()
        : base("Production")
    {
    }
}

/// <summary>
/// سناریوهای یکپارچه API پایش. میزبان آزمایشی پایگاه JSON را در پوشه موقت می‌سازد.
/// </summary>
public class MonitoringApiTests(MonitoringApiFactory factory) : IClassFixture<MonitoringApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>
    /// سناریو: هنوز درخواست آزمایشی نرفته است.
    /// انتظار: پاسخ ۲۰۰ با Success و مشخصات فرآیند و مسیر فایل monitoring.json.
    /// </summary>
    [Fact(DisplayName = "نمای کلی، مشخصات فرآیند و مسیر پایگاه فایل را برمی‌گرداند")]
    public async Task Overview_returns_process_identity_and_database_path()
    {
        var overview = await ReadAsync<MonitoringOverviewDto>("/api/monitoring/overview");

        Assert.True(overview.ProcessorCount > 0);
        Assert.True(overview.ProcessId > 0);
        Assert.False(string.IsNullOrWhiteSpace(overview.MachineName));
        Assert.EndsWith("monitoring.json", overview.DatabasePath);
        Assert.True(File.Exists(overview.DatabasePath));
    }

    /// <summary>
    /// سناریو: سه بار ping زده می‌شود.
    /// انتظار: شمارش همان مسیر دقیقاً سه تا زیاد شود و حجم پاسخ بزرگ‌تر از صفر باشد.
    /// </summary>
    [Fact(DisplayName = "هر ping در متریک مسیر /api/demo/ping ثبت می‌شود")]
    public async Task Ping_is_counted_on_its_route()
    {
        var before = await RouteCountAsync("GET", "/api/demo/ping");

        await _client.GetAsync("/api/demo/ping");
        await _client.GetAsync("/api/demo/ping");
        await _client.GetAsync("/api/demo/ping");

        var dashboard = await ReadAsync<DashboardDto>("/api/monitoring/dashboard");
        var route = Assert.Single(dashboard.Routes, item => item.HttpMethod == "GET" && item.Route == "/api/demo/ping");
        Assert.Equal(3, route.RequestCount - before);
        Assert.True(route.TotalResponseBytes > 0);
    }

    /// <summary>
    /// سناریو: محاسبه با تکرار یکسان دو بار صدا زده می‌شود.
    /// انتظار: هر دو نتیجه برابر و غیرصفر باشند و مسیر compute جدا از ping بماند.
    /// </summary>
    [Fact(DisplayName = "محاسبه آزمایشی نتیجه پایدار و مسیر جداگانه دارد")]
    public async Task Compute_returns_stable_checksum_and_its_own_route()
    {
        var first = await ReadAsync<DemoWorkloadResultDto>("/api/demo/compute?iterations=1000");
        var second = await ReadAsync<DemoWorkloadResultDto>("/api/demo/compute?iterations=1000");
        var routes = await ReadAsync<IReadOnlyList<RouteMetricDto>>("/api/monitoring/routes");

        Assert.Equal(first.Detail, second.Detail);
        Assert.NotEqual("0", first.Detail);
        Assert.Contains(routes, route => route.Route == "/api/demo/compute");
    }

    /// <summary>
    /// سناریو: یک مگابایت حافظه تخصیص داده می‌شود.
    /// انتظار: پاسخ همان تعداد بایت را بگوید و سهم حافظه مسیر حداقل یک مگابایت باشد.
    /// </summary>
    [Fact(DisplayName = "تخصیص حافظه در پاسخ و در سهم مسیر دیده می‌شود")]
    public async Task Allocate_reports_bytes_and_attributes_memory()
    {
        var result = await ReadAsync<DemoWorkloadResultDto>("/api/demo/allocate?megabytes=1");
        var routes = await ReadAsync<IReadOnlyList<RouteMetricDto>>("/api/monitoring/routes?windowMinutes=60");
        var route = Assert.Single(routes, item => item.Route == "/api/demo/allocate");

        Assert.Equal((1024 * 1024).ToString(), result.Detail);
        Assert.True(route.TotalAllocatedBytes >= 1024 * 1024);
    }

    /// <summary>
    /// سناریو: تأخیر ۲۰۰ میلی‌ثانیه‌ای. مسیر ممکن است درخواست نامعتبر سریع هم داشته باشد.
    /// انتظار: خود درخواست حداقل ۱۵۰ میلی‌ثانیه طول بکشد و حداکثر زمان ثبت‌شده مسیر هم از این کمتر نباشد.
    /// </summary>
    [Fact(DisplayName = "تأخیر آزمایشی در زمان پاسخ مسیر منعکس می‌شود")]
    public async Task Delay_is_visible_as_route_latency()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/demo/delay?milliseconds=200");
        watch.Stop();
        response.EnsureSuccessStatusCode();

        var routes = await ReadAsync<IReadOnlyList<RouteMetricDto>>("/api/monitoring/routes");
        var route = Assert.Single(routes, item => item.Route == "/api/demo/delay");

        Assert.True(watch.ElapsedMilliseconds >= 150);
        Assert.True(route.MaxElapsedMilliseconds >= 150);
    }

    /// <summary>
    /// سناریو: مسیر خطای آزمایشی.
    /// انتظار: میان‌افزار بدنه ۴۰۴ با Success=false بسازد و شمارش خطای همان مسیر یکی زیاد شود.
    /// </summary>
    [Fact(DisplayName = "خطای آزمایشی ۴۰۴ است و در آمار خطا می‌آید")]
    public async Task Fail_returns_middleware_not_found_and_counts_as_error()
    {
        var before = await ErrorCountAsync("/api/demo/fail");
        var response = await _client.GetAsync("/api/demo/fail");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        var after = await ErrorCountAsync("/api/demo/fail");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal(404, error.StatusCode);
        Assert.Equal("یافت نشد", error.Title);
        Assert.Equal(1, after - before);
    }

    /// <summary>
    /// سناریو: استثنای پیش‌بینی‌نشده در محیط توسعه.
    /// انتظار: وضعیت ۵۰۰ و پیام خود استثنا، بدون قالب پاسخ موفق.
    /// </summary>
    [Fact(DisplayName = "خرابی پیش‌بینی‌نشده در توسعه با پیام خطا و کد ۵۰۰ برمی‌گردد")]
    public async Task Crash_is_converted_to_internal_error_by_middleware()
    {
        var response = await _client.GetAsync("/api/demo/crash");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("خرابی پیش‌بینی‌نشده", error.Message);
    }

    /// <summary>
    /// سناریو: مسیری که کنترلر ندارد.
    /// انتظار: همان میان‌افزار ۴۰۴ برگرداند، نه صفحه پیش‌فرض خالی.
    /// </summary>
    [Fact(DisplayName = "مسیر ناشناخته از میان‌افزار خطا ۴۰۴ می‌گیرد")]
    public async Task Unknown_route_returns_not_found_payload()
    {
        var response = await _client.GetAsync("/api/does-not-exist");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal(404, error.StatusCode);
        Assert.False(error.Success);
    }

    /// <summary>
    /// سناریو: بازه صفر، بازه بیش از سقف، و پارامترهای نامعتبر بار آزمایشی.
    /// انتظار: همه ۴۰۰ با عنوان اعتبارسنجی باشند.
    /// </summary>
    [Theory(DisplayName = "ورودی نامعتبر اعتبارسنجی ۴۰۰ برمی‌گرداند")]
    [InlineData("/api/monitoring/dashboard?windowMinutes=0")]
    [InlineData("/api/monitoring/routes?windowMinutes=20000")]
    [InlineData("/api/demo/compute?iterations=0")]
    [InlineData("/api/demo/allocate?megabytes=500")]
    [InlineData("/api/demo/delay?milliseconds=0")]
    public async Task Invalid_input_returns_validation_error(string path)
    {
        var response = await _client.GetAsync(path);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal("اعتبارسنجی", error.Title);
        Assert.Equal(400, error.StatusCode);
    }

    /// <summary>
    /// سناریو: بعد از ping گزارش HTML ساخته می‌شود.
    /// انتظار: فایل خودکفا داده را داخل خودش داشته باشد و نام مسیر را نشان بدهد.
    /// </summary>
    [Fact(DisplayName = "گزارش HTML داده را درون خودش جاسازی می‌کند")]
    public async Task Report_embeds_dashboard_data()
    {
        await _client.GetAsync("/api/demo/ping");
        var response = await _client.GetAsync("/api/monitoring/report?windowMinutes=60");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("window.__EMBEDDED_REPORT__", html);
        Assert.Contains("/api/demo/ping", html);
        Assert.Contains("پایش سرور", html);
    }

    /// <summary>
    /// سناریو: دانلود گزارش.
    /// انتظار: هدر پیوست با نام فایل html وجود داشته باشد.
    /// </summary>
    [Fact(DisplayName = "دانلود گزارش هدر پیوست HTML دارد")]
    public async Task Report_download_sets_content_disposition()
    {
        var response = await _client.GetAsync("/api/monitoring/report?download=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.EndsWith(".html", response.Content.Headers.ContentDisposition?.FileName);
    }

    /// <summary>
    /// سناریو: کمی صبر تا نمونه‌بردار یک نقطه ثبت کند.
    /// انتظار: فهرست نمونه‌ها دست‌کم یک رکورد با حافظه مثبت داشته باشد.
    /// </summary>
    [Fact(DisplayName = "نمونه‌بردار نقطه پردازنده و حافظه ثبت می‌کند")]
    public async Task Sampler_produces_a_system_sample()
    {
        IReadOnlyList<SystemSampleDto>? samples = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            samples = await ReadAsync<IReadOnlyList<SystemSampleDto>>("/api/monitoring/samples");
            if (samples.Count > 0)
                break;

            await Task.Delay(200);
        }

        Assert.NotNull(samples);
        Assert.NotEmpty(samples);
        Assert.True(samples[0].WorkingSetBytes > 0);
    }

    /// <summary>
    /// سناریو: یک ping و سپس ذخیره صریح.
    /// انتظار: فایل monitoring.json کنار پایگاه همان مسیر را داشته باشد.
    /// </summary>
    [Fact(DisplayName = "ذخیره، درخواست را در فایل JSON کنار برنامه می‌نویسد")]
    public async Task Flush_writes_json_database_beside_the_app()
    {
        await _client.GetAsync("/api/demo/ping");
        await using var scope = factory.Services.CreateAsyncScope();
        var persistence = scope.ServiceProvider.GetRequiredService<IMetricPersistenceService>();
        await persistence.FlushAsync(CancellationToken.None);

        var json = await File.ReadAllTextAsync(Path.Combine(factory.DataDirectory, "monitoring.json"));
        Assert.Contains("/api/demo/ping", json);
        Assert.Contains("\"requests\"", json);
    }

    /// <summary>
    /// سناریو: یک رکورد کهنه‌تر از نگهداری و یک رکورد تازه در حافظه هستند.
    /// انتظار: بعد از پاک‌سازی فقط رکورد تازه در فایل بماند.
    /// </summary>
    [Fact(DisplayName = "پاک‌سازی، رکورد کهنه را از فایل JSON حذف می‌کند")]
    public async Task Trim_removes_old_requests_from_the_json_file()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<MonitoringStore>();
        store.AddRequest(new RequestTrace
        {
            Route = "/old-trace",
            HttpMethod = "GET",
            StatusCode = 200,
            OccurredAtUtc = DateTime.UtcNow.AddDays(-3)
        });
        store.AddRequest(new RequestTrace
        {
            Route = "/fresh-trace",
            HttpMethod = "GET",
            StatusCode = 200,
            OccurredAtUtc = DateTime.UtcNow
        });

        var persistence = scope.ServiceProvider.GetRequiredService<IMetricPersistenceService>();
        await persistence.TrimAsync(CancellationToken.None);
        var json = await File.ReadAllTextAsync(Path.Combine(factory.DataDirectory, "monitoring.json"));

        Assert.DoesNotContain("/old-trace", json);
        Assert.Contains("/fresh-trace", json);
    }

    private async Task<T> ReadAsync<T>(string path)
    {
        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<BaseResponseDto<T>>(JsonOptions);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        return body.Data;
    }

    private async Task<long> RouteCountAsync(string method, string route)
    {
        var routes = await ReadAsync<IReadOnlyList<RouteMetricDto>>("/api/monitoring/routes");
        return routes.FirstOrDefault(item => item.HttpMethod == method && item.Route == route)?.RequestCount ?? 0;
    }

    private async Task<long> ErrorCountAsync(string route)
    {
        var routes = await ReadAsync<IReadOnlyList<RouteMetricDto>>("/api/monitoring/routes");
        return routes.FirstOrDefault(item => item.Route == route)?.ErrorCount ?? 0;
    }
}

public class ProductionMonitoringApiTests(ProductionMonitoringApiFactory factory) : IClassFixture<ProductionMonitoringApiFactory>
{
    /// <summary>
    /// سناریو: همان خرابی پیش‌بینی‌نشده، این بار در محیط تولید.
    /// انتظار: پیام داخلی عمومی باشد و متن استثنا به کلاینت نرسد.
    /// </summary>
    [Fact(DisplayName = "در تولید، خطای پیش‌بینی‌نشده پیام داخلی را لو نمی‌دهد")]
    public async Task Production_hides_unexpected_exception_message()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/demo/crash");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("خطای داخلی سرور", error.Message);
        Assert.DoesNotContain("خرابی پیش‌بینی‌نشده", error.Message);
    }
}

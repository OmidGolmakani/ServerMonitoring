using Microsoft.Extensions.Options;

namespace ServerMonitoring.Application.Options;

public sealed class MonitoringOptionsValidator : IValidateOptions<MonitoringOptions>
{
    public ValidateOptionsResult Validate(string? name, MonitoringOptions options)
    {
        var failures = new List<string>();

        if (options.SampleIntervalMilliseconds is < 200 or > 60_000)
            failures.Add("SampleIntervalMilliseconds باید بین ۲۰۰ و ۶۰۰۰۰ میلی‌ثانیه باشد.");

        if (options.FlushIntervalMilliseconds is < 200 or > 300_000)
            failures.Add("FlushIntervalMilliseconds باید بین ۲۰۰ و ۳۰۰۰۰۰ میلی‌ثانیه باشد.");

        if (options.RetentionHours is < 1 or > 168)
            failures.Add("RetentionHours باید بین ۱ و ۱۶۸ ساعت باشد.");

        if (options.LiveRequestCapacity is < 100 or > 1_000_000)
            failures.Add("LiveRequestCapacity خارج از محدوده مجاز است.");

        if (options.LiveSampleCapacity is < 10 or > 100_000)
            failures.Add("LiveSampleCapacity خارج از محدوده مجاز است.");

        if (options.PendingCapacity is < 100 or > 1_000_000)
            failures.Add("PendingCapacity خارج از محدوده مجاز است.");

        if (options.PersistenceBatchSize is < 1 or > 5_000)
            failures.Add("PersistenceBatchSize خارج از محدوده مجاز است.");

        if (options.MaxWindowMinutes is < 1 or > 10_080)
            failures.Add("MaxWindowMinutes خارج از محدوده مجاز است.");

        if (options.DefaultWindowMinutes < 1 || options.DefaultWindowMinutes > options.MaxWindowMinutes)
            failures.Add("DefaultWindowMinutes باید بین ۱ و MaxWindowMinutes باشد.");

        if (options.DefaultSamplePoints is < 2 or > 2_000)
            failures.Add("DefaultSamplePoints خارج از محدوده مجاز است.");

        if (options.RecentRequestLimit is < 1 or > 500)
            failures.Add("RecentRequestLimit خارج از محدوده مجاز است.");

        if (string.IsNullOrWhiteSpace(options.DataDirectory))
            failures.Add("DataDirectory نباید خالی باشد.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

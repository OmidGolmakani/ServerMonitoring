using System.Globalization;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Workloads;

public sealed class DemoWorkloadService : IDemoWorkloadService
{
    public DemoWorkloadResultDto Ping()
        => new()
        {
            Operation = "ping",
            Detail = "pong"
        };

    public DemoWorkloadResultDto Compute(int iterations)
    {
        if (iterations is < 1 or > 2_000_000)
        {
            throw new ValidationException(
                "تعداد تکرار محاسبه نامعتبر است.",
                new Dictionary<string, string[]>
                {
                    ["iterations"] = ["iterations باید بین ۱ و ۲۰۰۰۰۰۰ باشد."]
                });
        }

        var checksum = 0L;
        for (var index = 1; index <= iterations; index++)
            checksum = unchecked(checksum + ((index * 31L) ^ (checksum >> 3)));

        return new DemoWorkloadResultDto
        {
            Operation = "compute",
            Detail = checksum.ToString(CultureInfo.InvariantCulture)
        };
    }

    public DemoWorkloadResultDto Allocate(int megabytes)
    {
        if (megabytes is < 1 or > 64)
        {
            throw new ValidationException(
                "حجم تخصیص نامعتبر است.",
                new Dictionary<string, string[]>
                {
                    ["megabytes"] = ["megabytes باید بین ۱ و ۶۴ باشد."]
                });
        }

        var length = megabytes * 1024 * 1024;
        var buffer = GC.AllocateUninitializedArray<byte>(length);
        for (var index = 0; index < buffer.Length; index += 4096)
            buffer[index] = (byte)(index % 251);

        return new DemoWorkloadResultDto
        {
            Operation = "allocate",
            Detail = buffer.Length.ToString(CultureInfo.InvariantCulture)
        };
    }

    public async Task<DemoWorkloadResultDto> DelayAsync(int milliseconds, CancellationToken cancellationToken)
    {
        if (milliseconds is < 1 or > 3_000)
        {
            throw new ValidationException(
                "مدت تأخیر نامعتبر است.",
                new Dictionary<string, string[]>
                {
                    ["milliseconds"] = ["milliseconds باید بین ۱ و ۳۰۰۰ باشد."]
                });
        }

        await Task.Delay(milliseconds, cancellationToken);
        return new DemoWorkloadResultDto
        {
            Operation = "delay",
            Detail = milliseconds.ToString(CultureInfo.InvariantCulture)
        };
    }

    public DemoWorkloadResultDto Fail()
        => throw new NotFoundException("سناریوی خطای آزمایشی؛ این پاسخ باید از میان‌افزار خطا برگردد.");

    public DemoWorkloadResultDto Crash()
        => throw new InvalidOperationException("خرابی پیش‌بینی‌نشده برای آزمون میان‌افزار.");
}

using ServerMonitoring.Application.Monitoring;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Api.Tests;

public sealed class DomainStatusCalculatorTests
{
    /// <summary>
    /// سناریو: دو دامنه با چند بررسی؛ یکی بیشتر بالا بوده و یکی آخرین بار پایین است.
    /// انتظار: درصد آپتایم درست حساب شود و دامنهٔ پایین زودتر در فهرست بیاید.
    /// </summary>
    [Fact(DisplayName = "وضعیت دامنه‌ها از تاریخچه بررسی‌ها ساخته می‌شود")]
    public void Calculates_uptime_and_orders_down_first()
    {
        var targets = new[]
        {
            new DomainTargetOptions { Name = "alpha", Url = "https://alpha.test/" },
            new DomainTargetOptions { Name = "beta", Url = "https://beta.test/" }
        };
        var checks = new[]
        {
            new DomainCheck { Name = "alpha", Url = "https://alpha.test/", IsUp = true, CheckedAtUtc = DateTime.UtcNow.AddMinutes(-2), StatusCode = 200 },
            new DomainCheck { Name = "alpha", Url = "https://alpha.test/", IsUp = true, CheckedAtUtc = DateTime.UtcNow.AddMinutes(-1), StatusCode = 200 },
            new DomainCheck { Name = "beta", Url = "https://beta.test/", IsUp = true, CheckedAtUtc = DateTime.UtcNow.AddMinutes(-2), StatusCode = 200 },
            new DomainCheck { Name = "beta", Url = "https://beta.test/", IsUp = false, CheckedAtUtc = DateTime.UtcNow.AddMinutes(-1), Error = "timeout" }
        };

        var statuses = DomainStatusCalculator.Calculate(targets, checks);

        Assert.Equal(2, statuses.Count);
        Assert.Equal("beta", statuses[0].Name);
        Assert.False(statuses[0].IsUp);
        Assert.Equal(50, statuses[0].UpPercent);
        Assert.Equal("alpha", statuses[1].Name);
        Assert.True(statuses[1].IsUp);
        Assert.Equal(100, statuses[1].UpPercent);
    }
}

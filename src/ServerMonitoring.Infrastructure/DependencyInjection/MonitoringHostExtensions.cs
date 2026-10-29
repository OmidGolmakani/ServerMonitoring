using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Infrastructure.DependencyInjection;

public static class MonitoringHostExtensions
{
    public static async Task InitializeMonitoringAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var persistence = scope.ServiceProvider.GetRequiredService<IMetricPersistenceService>();
        await persistence.LoadHistoryAsync(CancellationToken.None);
    }
}

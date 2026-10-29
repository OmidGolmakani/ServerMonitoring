using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Infrastructure.Monitoring;
using ServerMonitoring.Infrastructure.Persistence;
using ServerMonitoring.Infrastructure.Repositories;

namespace ServerMonitoring.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IMonitoringDatabase, JsonMonitoringDatabase>();
        services.AddSingleton<MonitoringStore>();
        services.AddSingleton<ProcessResourceMonitor>();
        services.AddSingleton<IRequestInstrumentation>(provider => provider.GetRequiredService<ProcessResourceMonitor>());
        services.AddSingleton<ISystemSampleProbe>(provider => provider.GetRequiredService<ProcessResourceMonitor>());
        services.AddSingleton<IHostProcessInfo, HostProcessInfo>();
        services.AddScoped<IRequestTraceRepository, RequestTraceRepository>();
        services.AddScoped<ISystemSampleRepository, SystemSampleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMetricPersistenceService, MetricPersistenceService>();
        services.AddHostedService<MonitoringCollectorHostedService>();
        return services;
    }
}

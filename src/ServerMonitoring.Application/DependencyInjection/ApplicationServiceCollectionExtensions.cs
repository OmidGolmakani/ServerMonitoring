using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Mapping;
using ServerMonitoring.Application.Monitoring;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Application.Workloads;

namespace ServerMonitoring.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(config => config.AddProfile<MonitoringProfile>());
        services.AddSingleton<IValidateOptions<MonitoringOptions>, MonitoringOptionsValidator>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<ISystemMetricsCollector, SystemMetricsCollector>();
        services.AddSingleton<IDemoWorkloadService, DemoWorkloadService>();
        return services;
    }
}

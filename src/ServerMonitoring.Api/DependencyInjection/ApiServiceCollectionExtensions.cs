using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ServerMonitoring.Application.DependencyInjection;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Reporting;
using ServerMonitoring.Infrastructure.DependencyInjection;

namespace ServerMonitoring.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddServerMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton<IHtmlReportRenderer, HtmlReportRenderer>();
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        services.AddOpenApi();
        return services;
    }
}

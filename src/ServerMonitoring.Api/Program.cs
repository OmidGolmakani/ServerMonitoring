using ServerMonitoring.Api.DependencyInjection;
using ServerMonitoring.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServerMonitoring(builder.Configuration);

var app = builder.Build();

app.UseServerMonitoring();
await app.InitializeMonitoringAsync();

app.Run();

public partial class Program;

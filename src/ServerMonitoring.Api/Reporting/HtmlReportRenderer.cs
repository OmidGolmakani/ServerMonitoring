using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Reporting;

public sealed class HtmlReportRenderer(IWebHostEnvironment environment, IOptions<JsonOptions> jsonOptions) : IHtmlReportRenderer
{
    public string Render(DashboardDto dashboard)
    {
        var templatePath = Path.Combine(environment.WebRootPath, "index.html");
        if (!File.Exists(templatePath))
            throw new NotFoundException("قالب گزارش HTML پیدا نشد.");

        var html = File.ReadAllText(templatePath);
        const string marker = "<!--REPORT_DATA-->";
        if (!html.Contains(marker, StringComparison.Ordinal))
            throw new NotFoundException("نشانگر داده در قالب گزارش پیدا نشد.");

        var json = JsonSerializer.Serialize(dashboard, jsonOptions.Value.JsonSerializerOptions);
        json = json.Replace("<", "\\u003c", StringComparison.Ordinal);
        return html.Replace(marker, $"<script>window.__EMBEDDED_REPORT__ = {json};</script>", StringComparison.Ordinal);
    }
}

using Microsoft.AspNetCore.Mvc;
using ServerMonitoring.Application.Common;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
public sealed class MonitoringController(
    IMonitoringService monitoringService,
    IDomainCatalogService domainCatalogService) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(BaseResponseDto<DashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<DashboardDto>>> GetDashboard(
        [FromQuery] int? windowMinutes,
        CancellationToken cancellationToken)
    {
        var data = await monitoringService.GetDashboardAsync(windowMinutes, cancellationToken);
        return Ok(BaseResponseDto<DashboardDto>.Ok(data));
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(BaseResponseDto<MonitoringOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<MonitoringOverviewDto>>> GetOverview(
        [FromQuery] int? windowMinutes,
        CancellationToken cancellationToken)
    {
        var data = await monitoringService.GetOverviewAsync(windowMinutes, cancellationToken);
        return Ok(BaseResponseDto<MonitoringOverviewDto>.Ok(data));
    }

    [HttpGet("routes")]
    [ProducesResponseType(typeof(BaseResponseDto<IReadOnlyList<RouteMetricDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<IReadOnlyList<RouteMetricDto>>>> GetRoutes(
        [FromQuery] int? windowMinutes,
        CancellationToken cancellationToken)
    {
        var data = await monitoringService.GetRoutesAsync(windowMinutes, cancellationToken);
        return Ok(BaseResponseDto<IReadOnlyList<RouteMetricDto>>.Ok(data));
    }

    [HttpGet("samples")]
    [ProducesResponseType(typeof(BaseResponseDto<IReadOnlyList<SystemSampleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<IReadOnlyList<SystemSampleDto>>>> GetSamples(
        [FromQuery] int? windowMinutes,
        CancellationToken cancellationToken)
    {
        var data = await monitoringService.GetSamplesAsync(windowMinutes, cancellationToken);
        return Ok(BaseResponseDto<IReadOnlyList<SystemSampleDto>>.Ok(data));
    }

    [HttpGet("server")]
    [ProducesResponseType(typeof(BaseResponseDto<ServerOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<ServerOverviewDto>>> GetServer(
        [FromQuery] int? windowMinutes,
        CancellationToken cancellationToken)
    {
        var data = await monitoringService.GetServerAsync(windowMinutes, cancellationToken);
        return Ok(BaseResponseDto<ServerOverviewDto>.Ok(data));
    }

    [HttpGet("domains")]
    [ProducesResponseType(typeof(BaseResponseDto<IReadOnlyList<DomainStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<IReadOnlyList<DomainStatusDto>>>> GetDomains(
        [FromQuery] int? windowMinutes,
        CancellationToken cancellationToken)
    {
        var data = await monitoringService.GetDomainsAsync(windowMinutes, cancellationToken);
        return Ok(BaseResponseDto<IReadOnlyList<DomainStatusDto>>.Ok(data));
    }

    [HttpGet("domain-catalog")]
    [ProducesResponseType(typeof(BaseResponseDto<DomainCatalogDto>), StatusCodes.Status200OK)]
    public ActionResult<BaseResponseDto<DomainCatalogDto>> GetDomainCatalog()
    {
        var data = domainCatalogService.GetCatalog();
        return Ok(BaseResponseDto<DomainCatalogDto>.Ok(data));
    }

    [HttpPut("domain-catalog")]
    [ProducesResponseType(typeof(BaseResponseDto<DomainCatalogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponseDto<DomainCatalogDto>>> SaveDomainCatalog(
        [FromBody] SaveDomainSelectionRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await domainCatalogService.SaveSelectionAsync(request, cancellationToken);
        return Ok(BaseResponseDto<DomainCatalogDto>.Ok(data, "انتخاب دامنه‌ها ذخیره شد."));
    }

    [HttpGet("report")]
    [Produces("text/html")]
    public async Task<IActionResult> GetReport(
        [FromQuery] int? windowMinutes,
        [FromQuery] bool download,
        CancellationToken cancellationToken)
    {
        var html = await monitoringService.BuildHtmlReportAsync(windowMinutes, cancellationToken);
        if (!download)
            return Content(html, "text/html; charset=utf-8");

        var fileName = $"server-monitoring-{DateTime.Now:yyyyMMdd-HHmmss}.html";
        return File(System.Text.Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", fileName);
    }
}

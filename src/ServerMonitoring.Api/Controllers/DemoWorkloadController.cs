using Microsoft.AspNetCore.Mvc;
using ServerMonitoring.Application.Common;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Api.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoWorkloadController(IDemoWorkloadService demoWorkloadService) : ControllerBase
{
    [HttpGet("ping")]
    public ActionResult<BaseResponseDto<DemoWorkloadResultDto>> Ping()
    {
        var result = demoWorkloadService.Ping();
        return Ok(BaseResponseDto<DemoWorkloadResultDto>.Ok(result, "پاسخ آزمایشی آماده است."));
    }

    [HttpGet("compute")]
    public ActionResult<BaseResponseDto<DemoWorkloadResultDto>> Compute([FromQuery] int iterations = 250_000)
    {
        var result = demoWorkloadService.Compute(iterations);
        return Ok(BaseResponseDto<DemoWorkloadResultDto>.Ok(result, "محاسبه آزمایشی تمام شد."));
    }

    [HttpGet("allocate")]
    public ActionResult<BaseResponseDto<DemoWorkloadResultDto>> Allocate([FromQuery] int megabytes = 8)
    {
        var result = demoWorkloadService.Allocate(megabytes);
        return Ok(BaseResponseDto<DemoWorkloadResultDto>.Ok(result, "تخصیص حافظه آزمایشی تمام شد."));
    }

    [HttpGet("delay")]
    public async Task<ActionResult<BaseResponseDto<DemoWorkloadResultDto>>> Delay(
        [FromQuery] int milliseconds,
        CancellationToken cancellationToken)
    {
        var result = await demoWorkloadService.DelayAsync(milliseconds, cancellationToken);
        return Ok(BaseResponseDto<DemoWorkloadResultDto>.Ok(result, "تأخیر آزمایشی تمام شد."));
    }

    [HttpGet("fail")]
    public ActionResult<BaseResponseDto<DemoWorkloadResultDto>> Fail()
    {
        var result = demoWorkloadService.Fail();
        return Ok(BaseResponseDto<DemoWorkloadResultDto>.Ok(result));
    }

    [HttpGet("crash")]
    public ActionResult<BaseResponseDto<DemoWorkloadResultDto>> Crash()
    {
        var result = demoWorkloadService.Crash();
        return Ok(BaseResponseDto<DemoWorkloadResultDto>.Ok(result));
    }
}

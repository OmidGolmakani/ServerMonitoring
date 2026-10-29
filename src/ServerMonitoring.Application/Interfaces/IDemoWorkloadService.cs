using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IDemoWorkloadService
{
    DemoWorkloadResultDto Ping();

    DemoWorkloadResultDto Compute(int iterations);

    DemoWorkloadResultDto Allocate(int megabytes);

    Task<DemoWorkloadResultDto> DelayAsync(int milliseconds, CancellationToken cancellationToken);

    DemoWorkloadResultDto Fail();

    DemoWorkloadResultDto Crash();
}

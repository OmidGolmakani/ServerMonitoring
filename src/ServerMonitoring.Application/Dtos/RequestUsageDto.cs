namespace ServerMonitoring.Application.Dtos;

public sealed class RequestUsageDto
{
    public double CpuMilliseconds { get; init; }

    public long AllocatedBytes { get; init; }
}

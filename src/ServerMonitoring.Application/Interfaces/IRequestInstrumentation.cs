using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IRequestInstrumentation
{
    int ActiveRequests { get; }

    long Begin();

    RequestUsageDto Complete(long executionId);
}

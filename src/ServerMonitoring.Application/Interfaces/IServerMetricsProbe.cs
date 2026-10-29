using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IServerMetricsProbe
{
    NewServerSampleDto Capture();
}

using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface ISystemSampleProbe
{
    NewSystemSampleDto Capture();
}

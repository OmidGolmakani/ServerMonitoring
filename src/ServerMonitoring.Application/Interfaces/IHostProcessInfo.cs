using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IHostProcessInfo
{
    HostProcessInfoDto GetCurrent();
}

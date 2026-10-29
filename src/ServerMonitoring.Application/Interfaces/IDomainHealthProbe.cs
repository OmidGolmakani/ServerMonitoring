using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Application.Interfaces;

public interface IDomainHealthProbe
{
    Task<NewDomainCheckDto> CheckAsync(DomainTargetOptions target, CancellationToken cancellationToken);
}

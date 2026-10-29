using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Application.Interfaces;

public interface IDomainCatalog
{
    IReadOnlyList<DomainTargetOptions> GetTargets();

    DomainCatalogDto GetCatalog();

    void InvalidateCache();
}

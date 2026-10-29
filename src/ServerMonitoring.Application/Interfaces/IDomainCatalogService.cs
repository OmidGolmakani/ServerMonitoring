using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IDomainCatalogService
{
    DomainCatalogDto GetCatalog();

    Task<DomainCatalogDto> SaveSelectionAsync(SaveDomainSelectionRequestDto request, CancellationToken cancellationToken);
}

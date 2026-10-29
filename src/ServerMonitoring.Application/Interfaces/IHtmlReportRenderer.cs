using ServerMonitoring.Application.Dtos;

namespace ServerMonitoring.Application.Interfaces;

public interface IHtmlReportRenderer
{
    string Render(DashboardDto dashboard);
}

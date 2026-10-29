using System.Diagnostics;
using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class DomainHttpProbe : IDomainHealthProbe
{
    private readonly HttpClient _httpClient;

    public DomainHttpProbe(IHttpClientFactory httpClientFactory, IOptions<MonitoringOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("DomainProbe");
        _httpClient.Timeout = TimeSpan.FromMilliseconds(options.Value.DomainProbeTimeoutMilliseconds);
    }

    public async Task<NewDomainCheckDto> CheckAsync(DomainTargetOptions target, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, target.Url);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if ((int)response.StatusCode is 405 or 501)
            {
                using var getResponse = await _httpClient.GetAsync(
                    target.Url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                return CreateResult(target, getResponse.IsSuccessStatusCode, (int)getResponse.StatusCode, started, null);
            }

            return CreateResult(target, response.IsSuccessStatusCode, (int)response.StatusCode, started, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return CreateResult(target, false, null, started, exception.Message);
        }
    }

    private static NewDomainCheckDto CreateResult(
        DomainTargetOptions target,
        bool isUp,
        int? statusCode,
        long started,
        string? error)
    {
        return new NewDomainCheckDto
        {
            CheckedAtUtc = DateTime.UtcNow,
            Name = target.Name,
            Url = target.Url,
            IsUp = isUp,
            StatusCode = statusCode,
            ElapsedMilliseconds = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 2),
            Error = error
        };
    }
}

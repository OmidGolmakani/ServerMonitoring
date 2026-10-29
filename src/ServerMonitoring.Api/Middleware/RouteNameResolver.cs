using Microsoft.AspNetCore.Routing;

namespace ServerMonitoring.Api.Middleware;

internal static class RouteNameResolver
{
    public static string Resolve(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint endpoint && !string.IsNullOrWhiteSpace(endpoint.RoutePattern.RawText))
        {
            var raw = endpoint.RoutePattern.RawText;
            return raw.StartsWith('/') ? raw : "/" + raw;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        return path.Length <= 512 ? path : path[..512];
    }
}

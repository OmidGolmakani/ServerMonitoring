using ServerMonitoring.Api.Middleware;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Api.DependencyInjection;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseServerMonitoring(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
        });
        app.UseRouting();
        app.UseMiddleware<RequestMetricsMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.MapControllers();
        app.MapFallback((HttpContext _) => throw new NotFoundException("مسیر درخواستی پیدا نشد."));
        return app;
    }
}

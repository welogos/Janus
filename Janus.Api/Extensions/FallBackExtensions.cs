using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Enums;

namespace Janus.Api.Extensions;

public static class FallBackExtensions
{
    public static Task EndpointFallBackAsync(this WebApplication app)
    {
        try
        {
            return Task.FromResult(app.MapFallback("/{**path}", async (context) 
                => await EndpointFallBackConfiguration(context, app)));
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    private static async Task EndpointFallBackConfiguration(HttpContext context, WebApplication app)
    {
        var registry = app.Services.GetRequiredService<IEndpointRegistry>();
        var logger =  app.Services.GetRequiredService<ILogger<Program>>();
        
        var route = context.Request.Path.Value ?? "/";
        var requestMethod = context.Request.Method;

        logger.LogDebug(
            "Resolving incoming dynamic route {Route} for HTTP method {RequestMethod}.",
            route,
            requestMethod);

        if (!Enum.TryParse<EHttpMethods>(
                requestMethod,
                true,
                out var method))
        {
            logger.LogWarning(
                "HTTP method {RequestMethod} is not supported for dynamic route {Route}.",
                requestMethod,
                route);

            context.Response.StatusCode =
                StatusCodes.Status405MethodNotAllowed;

            return;
        }

        var endpoints = await registry.GetEndpointsAsync();

        logger.LogDebug(
            "Endpoint registry contains {EndpointCount} endpoints during dynamic route resolution.",
            endpoints?.Count ?? 0);

        var endpoint = await registry.FindAsync(route, method);

        if (endpoint is null)
        {
            logger.LogWarning(
                "Dynamic endpoint was not found for route {Route} and method {Method}.",
                route,
                method);

            context.Response.StatusCode =
                StatusCodes.Status404NotFound;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Dynamic endpoint was not found.",
                route,
                method = method.ToString()
            });

            return;
        }

        logger.LogDebug(
            "Dynamic endpoint {EndpointId} successfully resolved for route {Route} and method {Method}.",
            endpoint.Id,
            route,
            method);

        await context.Response.WriteAsJsonAsync(new
        {
            endpoint.Id,
            endpoint.ClientName,
            endpoint.ClientRoute,
            endpoint.Method
        });
    }
}
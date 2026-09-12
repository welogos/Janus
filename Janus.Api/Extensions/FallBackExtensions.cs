using Janus.Application.Interfaces.Services.Dispatching;
using Janus.Application.Models.Dispatching;

namespace Janus.Api.Extensions;

/// <summary>
/// Maps the HTTP adapter for dynamic endpoint dispatch.
/// </summary>
public static class FallBackExtensions
{
    /// <summary>
    /// Maps the catch-all route used after administrative controller routes have been evaluated.
    /// </summary>
    /// <param name="app">The Janus web application.</param>
    public static Task EndpointFallBackAsync(this WebApplication app)
    {
        app.MapFallback("/{**path}", DispatchAsync);
        return Task.CompletedTask;
    }

    private static async Task DispatchAsync(HttpContext context)
    {
        var resolver = context.RequestServices.GetRequiredService<IEndpointResolver>();
        var dispatcher = context.RequestServices.GetRequiredService<IEndpointDispatcher>();
        var route = context.Request.Path.Value ?? "/";
        var resolution = await resolver.ResolveAsync(route, context.Request.Method);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName] =
            context.TraceIdentifier;

        if (resolution.Status == EndpointResolutionStatus.NotFound)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (resolution.Status == EndpointResolutionStatus.MethodNotAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = string.Join(
                ", ",
                resolution.AllowedMethods.Select(method => method.ToString().ToUpperInvariant()));
            return;
        }

        var endpoint = resolution.Endpoint
            ?? throw new InvalidOperationException(
                "Endpoint resolution returned no endpoint for a successful result.");

        var request = new DispatchRequest(
            route,
            context.Request.Method,
            context.Request.QueryString.Value ?? string.Empty,
            await ReadBodyAsync(context.Request, context.RequestAborted),
            context.Request.Headers.ToDictionary(
                header => header.Key,
                header => (IReadOnlyList<string>)header.Value
                    .Select(value => value ?? string.Empty)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase),
            context.TraceIdentifier);

        var response = await dispatcher.DispatchAsync(
            endpoint,
            request,
            context.RequestAborted);

        context.Response.StatusCode = response.StatusCode;

        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        if (!response.Body.IsEmpty)
            await context.Response.Body.WriteAsync(response.Body, context.RequestAborted);
    }

    private static async Task<ReadOnlyMemory<byte>> ReadBodyAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength == 0)
            return ReadOnlyMemory<byte>.Empty;

        await using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}

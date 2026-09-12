using Janus.Application.Interfaces.Services.Dispatching;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Application.Models.Dispatching;
using Janus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Janus.Infrastructure.Services.Dispatching;

/// <summary>
/// Resolves dynamic endpoints through the existing runtime endpoint registry.
/// </summary>
public sealed class EndpointResolver(
    IEndpointRegistry registry,
    ILogger<EndpointResolver> logger) : IEndpointResolver
{
    /// <inheritdoc />
    public async Task<EndpointResolution> ResolveAsync(string route, string httpMethod)
    {
        var routeEndpoints = await registry.FindByRouteAsync(route);

        if (routeEndpoints.Count == 0)
        {
            logger.LogInformation(
                "No enabled dynamic endpoint was found for route {Route} and method {Method}.",
                route,
                httpMethod);

            return new EndpointResolution(
                EndpointResolutionStatus.NotFound,
                null,
                []);
        }

        var allowedMethods = routeEndpoints
            .Select(endpoint => endpoint.Method)
            .Distinct()
            .OrderBy(method => method)
            .ToArray();

        if (!Enum.TryParse<EHttpMethods>(httpMethod, true, out var method))
            return MethodNotAllowed(route, httpMethod, allowedMethods);

        var endpoint = await registry.FindAsync(route, method);

        if (endpoint is null)
            return MethodNotAllowed(route, httpMethod, allowedMethods);

        logger.LogInformation(
            "Resolved dynamic endpoint {EndpointId} for route {Route} and method {Method}. Destination={Destination}.",
            endpoint.Id,
            route,
            httpMethod,
            endpoint.ClientName);

        return new EndpointResolution(
            EndpointResolutionStatus.Found,
            endpoint,
            allowedMethods);
    }

    private EndpointResolution MethodNotAllowed(
        string route,
        string httpMethod,
        IReadOnlyList<EHttpMethods> allowedMethods)
    {
        logger.LogInformation(
            "Dynamic route {Route} does not accept method {Method}. AllowedMethods={AllowedMethods}.",
            route,
            httpMethod,
            string.Join(", ", allowedMethods));

        return new EndpointResolution(
            EndpointResolutionStatus.MethodNotAllowed,
            null,
            allowedMethods);
    }
}

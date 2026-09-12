using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Janus.Infrastructure.Services.Endpoints;

public class EndpointRegistry(IMemoryCache cache, ILogger<EndpointRegistry> logger) : IEndpointRegistry
{
    private const string CacheKey = "janus:endpoints";
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Task<List<EndpointDomain>?> GetEndpointsAsync()
    {
        var endpoints = cache.Get<List<EndpointDomain>>(CacheKey);

        logger.LogDebug(
            "Retrieved {EndpointCount} endpoints from the endpoint registry. RegistryInitialized={RegistryInitialized}.",
            endpoints?.Count ?? 0,
            endpoints is not null);

        return Task.FromResult(endpoints);
    }

    public async Task<EndpointDomain?> GetEndpointAsync(Guid id)
    {
        logger.LogDebug(
            "Looking up endpoint {EndpointId} in the endpoint registry.",
            id);

        var endpoints = await GetEndpointsAsync();
        var endpoint = endpoints?.FirstOrDefault(x => x.Id == id);

        logger.LogDebug(
            "Endpoint registry lookup for {EndpointId} completed. EndpointFound={EndpointFound}.",
            id,
            endpoint is not null);

        return endpoint;
    }

    public async Task<EndpointDomain?> FindAsync(
        string route,
        EHttpMethods method)
    {
        logger.LogDebug(
            "Resolving endpoint for route {Route} and method {Method}.",
            route,
            method);

        var endpoints = await GetEndpointsAsync();

        route = EndpointRoute.Normalize(route);

        var endpoint = endpoints?
            .Where(x =>
                x.Enabled &&
                EndpointRoute.Normalize(x.ClientRoute) == route &&
                x.Method == method)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        logger.LogDebug(
            "Endpoint resolution for route {Route} and method {Method} completed. EndpointFound={EndpointFound}.",
            route,
            method,
            endpoint is not null);

        return endpoint;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EndpointDomain>> FindByRouteAsync(string route)
    {
        var endpoints = await GetEndpointsAsync();
        var normalizedRoute = EndpointRoute.Normalize(route);

        return endpoints?
            .Where(endpoint =>
                endpoint.Enabled &&
                EndpointRoute.Normalize(endpoint.ClientRoute) == normalizedRoute)
            .OrderBy(endpoint => endpoint.Method)
            .ThenBy(endpoint => endpoint.CreatedAt)
            .ThenBy(endpoint => endpoint.Id)
            .ToArray()
            ?? [];
    }

    public async Task SetEndpointAsync(EndpointDomain endpoint)
    {
        await _lock.WaitAsync();

        try
        {
            var endpoints = await GetEndpointsAsync()
                ?? throw new InvalidOperationException(
                    "The endpoint registry has not been initialized.");

            endpoints.RemoveAll(x => x.Id == endpoint.Id);

            if (endpoint.Enabled)
                endpoints.Add(endpoint);

            LoadEndpoints(endpoints);

            logger.LogInformation(
                "Endpoint {EndpointId} synchronized with the endpoint registry. Enabled={EndpointEnabled}.",
                endpoint.Id,
                endpoint.Enabled);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveEndpointAsync(Guid id)
    {
        await _lock.WaitAsync();

        try
        {
            var endpoints = await GetEndpointsAsync()
                ?? throw new InvalidOperationException(
                    "The endpoint registry has not been initialized.");

            var removed = endpoints.RemoveAll(x => x.Id == id);

            if (removed == 0)
                throw new KeyNotFoundException(
                    $"Endpoint with id '{id}' was not found in the endpoint registry.");

            LoadEndpoints(endpoints);

            logger.LogInformation(
                "Endpoint {EndpointId} removed from the endpoint registry.",
                id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task LoadEndpointsAsync(List<EndpointDomain> endpoints)
    {
        LoadEndpoints(endpoints);

        logger.LogInformation(
            "Endpoint registry initialized with {EndpointCount} enabled endpoints.",
            endpoints.Count(x => x.Enabled));

        return Task.CompletedTask;
    }

    private void LoadEndpoints(List<EndpointDomain> endpoints)
    {
        cache.Set(
            CacheKey,
            endpoints.Where(x => x.Enabled).ToList());
    }

}

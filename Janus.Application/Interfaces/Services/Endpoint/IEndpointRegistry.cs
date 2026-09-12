using Janus.Domain.Enums;
using Janus.Domain.Models;

namespace Janus.Application.Interfaces.Services.Endpoint;

/// <summary>
/// Defines operations for managing endpoints in the runtime endpoint registry.
/// </summary>
public interface IEndpointRegistry
{
    /// <summary>
    /// Adds or replaces an endpoint in the registry.
    /// </summary>
    /// <param name="endpoint">The endpoint to store.</param>
    Task SetEndpointAsync(EndpointDomain endpoint);

    /// <summary>
    /// Retrieves all endpoints currently loaded in the registry.
    /// </summary>
    /// <returns>The loaded endpoints, or <c>null</c> when the registry has not been initialized.</returns>
    Task<List<EndpointDomain>?> GetEndpointsAsync();

    /// <summary>
    /// Retrieves an endpoint from the registry by its identifier.
    /// </summary>
    /// <param name="id">The endpoint identifier.</param>
    /// <returns>The endpoint when found; otherwise, <c>null</c>.</returns>
    Task<EndpointDomain?> GetEndpointAsync(Guid id);

    /// <summary>
    /// Removes an endpoint from the registry.
    /// </summary>
    /// <param name="id">The endpoint identifier.</param>
    Task RemoveEndpointAsync(Guid id);

    /// <summary>
    /// Replaces the registry contents with the supplied endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoints to load.</param>
    Task LoadEndpointsAsync(List<EndpointDomain> endpoints);

    /// <summary>
    /// Resolves an enabled endpoint by route and HTTP method.
    /// </summary>
    /// <param name="route">The client route to resolve.</param>
    /// <param name="method">The HTTP method to match.</param>
    /// <returns>The matching endpoint when found; otherwise, <c>null</c>.</returns>
    Task<EndpointDomain?> FindAsync(string route, EHttpMethods method);

    /// <summary>
    /// Retrieves all enabled endpoints matching a normalized public route.
    /// </summary>
    /// <param name="route">The public route to resolve.</param>
    /// <returns>The matching enabled endpoints.</returns>
    Task<IReadOnlyList<EndpointDomain>> FindByRouteAsync(string route);
}

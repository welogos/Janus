using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;

namespace Janus.Application.Interfaces.Services.Endpoint;

/// <summary>
/// Defines application operations for creating, deleting, and retrieving endpoints.
/// </summary>
public interface IEndpointService
{
    /// <summary>
    /// Creates an endpoint from the supplied data.
    /// </summary>
    /// <param name="dto">The endpoint data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CreateEndpointAsync(EndpointDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the endpoint with the specified identifier.
    /// </summary>
    /// <param name="id">The endpoint identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task DeleteEndpointAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the endpoint with the specified identifier.
    /// </summary>
    /// <param name="id">The endpoint identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The matching endpoint.</returns>
    Task<EndpointDomain> GetEndpointAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the available endpoints.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The available endpoints.</returns>
    Task<List<EndpointDomain>> GetEndpointsAsync(CancellationToken cancellationToken);
}

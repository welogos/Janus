using Janus.Application.Models.Dispatching;
using Janus.Domain.Models;

namespace Janus.Application.Interfaces.Services.Dispatching;

/// <summary>
/// Forwards a request to the trusted destination configured for a dynamic endpoint.
/// </summary>
public interface IEndpointDispatcher
{
    /// <summary>
    /// Dispatches a request and returns the safe upstream response.
    /// </summary>
    /// <param name="endpoint">The resolved endpoint configuration.</param>
    /// <param name="request">The incoming request data.</param>
    /// <param name="cancellationToken">The client request cancellation token.</param>
    /// <returns>The response received from the configured destination.</returns>
    Task<DispatchResponse> DispatchAsync(
        EndpointDomain endpoint,
        DispatchRequest request,
        CancellationToken cancellationToken);
}

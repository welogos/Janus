using Janus.Application.Models.Dispatching;

namespace Janus.Application.Interfaces.Services.Dispatching;

/// <summary>
/// Resolves enabled dynamic endpoints from the runtime endpoint registry.
/// </summary>
public interface IEndpointResolver
{
    /// <summary>
    /// Resolves an endpoint by public route and HTTP method.
    /// </summary>
    /// <param name="route">The public route received by Janus.</param>
    /// <param name="httpMethod">The incoming HTTP method.</param>
    /// <returns>The endpoint resolution result.</returns>
    Task<EndpointResolution> ResolveAsync(string route, string httpMethod);
}

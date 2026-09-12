using Janus.Domain.Enums;
using Janus.Domain.Models;

namespace Janus.Application.Models.Dispatching;

/// <summary>
/// Describes the result of resolving a public route and HTTP method.
/// </summary>
/// <param name="Status">The resolution outcome.</param>
/// <param name="Endpoint">The resolved endpoint when the outcome is found.</param>
/// <param name="AllowedMethods">The enabled methods available for the public route.</param>
public sealed record EndpointResolution(
    EndpointResolutionStatus Status,
    EndpointDomain? Endpoint,
    IReadOnlyList<EHttpMethods> AllowedMethods);

/// <summary>
/// Defines the possible dynamic endpoint resolution outcomes.
/// </summary>
public enum EndpointResolutionStatus
{
    /// <summary>The route and method resolved to an enabled endpoint.</summary>
    Found,

    /// <summary>No enabled endpoint exists for the route.</summary>
    NotFound,

    /// <summary>The route exists, but it does not accept the requested method.</summary>
    MethodNotAllowed,
}

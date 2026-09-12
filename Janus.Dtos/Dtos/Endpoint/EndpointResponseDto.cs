using Janus.Domain.Enums;
using Janus.Domain.Models;

namespace Janus.Dtos.Dtos.Endpoint;

/// <summary>
/// Represents the public administrative view of a dynamic endpoint without its internal destination.
/// </summary>
public sealed class EndpointResponseDto
{
    /// <summary>Gets the endpoint identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the logical client name.</summary>
    public string ClientName { get; init; } = string.Empty;

    /// <summary>Gets the public route.</summary>
    public string ClientRoute { get; init; } = string.Empty;

    /// <summary>Gets the accepted HTTP method.</summary>
    public EHttpMethods Method { get; init; }

    /// <summary>Gets whether the endpoint is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the endpoint creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Creates the public view from a domain endpoint.
    /// </summary>
    /// <param name="endpoint">The domain endpoint.</param>
    /// <returns>The public administrative view.</returns>
    public static EndpointResponseDto FromDomain(EndpointDomain endpoint)
        => new()
        {
            Id = endpoint.Id,
            ClientName = endpoint.ClientName,
            ClientRoute = endpoint.ClientRoute,
            Method = endpoint.Method,
            Enabled = endpoint.Enabled,
            CreatedAt = endpoint.CreatedAt,
        };
}

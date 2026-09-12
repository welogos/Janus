namespace Janus.Application.Exceptions;

/// <summary>
/// Represents a timeout while contacting a configured endpoint destination.
/// </summary>
public sealed class EndpointDispatchTimeoutException : Exception
{
    /// <summary>Creates an endpoint dispatch timeout exception.</summary>
    public EndpointDispatchTimeoutException()
        : base("The destination service did not respond in time.")
    {
    }
}

/// <summary>
/// Represents an unavailable or invalid response from a configured endpoint destination.
/// </summary>
public sealed class EndpointUpstreamException : Exception
{
    /// <summary>Creates an upstream dispatch exception.</summary>
    public EndpointUpstreamException()
        : base("The destination service is unavailable.")
    {
    }
}

/// <summary>
/// Represents an invalid trusted destination configuration.
/// </summary>
public sealed class EndpointConfigurationException : Exception
{
    /// <summary>Creates an endpoint configuration exception.</summary>
    public EndpointConfigurationException()
        : base("The endpoint destination configuration is invalid.")
    {
    }
}

using Janus.Domain.Enums;

namespace Janus.Domain.Models;

public class EndpointDomain : Entity
{
    #region Attributes
    public Guid Id             { get; private set; }
    public string ClientName   { get; private set; } = string.Empty;
    public string ClientRoute  { get; private set; } = string.Empty;
    /// <summary>
    /// Gets the trusted absolute URL used to forward requests for this endpoint.
    /// </summary>
    public string TargetUrl    { get; private set; } = string.Empty;
    public EHttpMethods Method { get; private set; }
    public bool Enabled        { get; private set; }
    public DateTime CreatedAt  { get; private set; }
    #endregion

    #region Constructors
    public EndpointDomain(){} // for EF.

    /// <summary>
    /// Creates a dynamic endpoint configuration.
    /// </summary>
    /// <param name="clientName">The logical client or destination name.</param>
    /// <param name="clientRoute">The public route exposed by Janus.</param>
    /// <param name="method">The accepted HTTP method.</param>
    /// <param name="enabled">Whether the endpoint can be resolved.</param>
    /// <param name="targetUrl">The trusted absolute destination URL.</param>
    public EndpointDomain(
        string clientName,
        string clientRoute,
        EHttpMethods method,
        bool enabled = true,
        string targetUrl = "")
    {
        Id          =  Guid.NewGuid();
        ClientName  = clientName;
        ClientRoute = EndpointRoute.Normalize(clientRoute);
        TargetUrl    = targetUrl.Trim();
        Method      = method;   
        Enabled     = enabled;
        CreatedAt   = DateTime.UtcNow;
        UpdatedAt   =  DateTime.UtcNow;
    }
    #endregion
}

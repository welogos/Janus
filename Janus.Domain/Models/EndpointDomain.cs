using Janus.Domain.Enums;

namespace Janus.Domain.Models;

public class EndpointDomain : Entity
{
    #region Attributes
    public Guid Id             { get; private set; }
    public string ClientName   { get; private set; } = string.Empty;
    public string ClientRoute  { get; private set; } = string.Empty;
    public EHttpMethods Method { get; private set; }
    public bool Enabled        { get; private set; }
    public DateTime CreatedAt  { get; private set; }
    #endregion

    #region Constructors
    public EndpointDomain(){} // for EF.

    public EndpointDomain(string clientName, string clientRoute, EHttpMethods method,  bool enabled=true)
    {
        Id          =  Guid.NewGuid();
        ClientName  = clientName;
        ClientRoute = clientRoute;
        Method      = method;   
        Enabled     = enabled;
        CreatedAt   = DateTime.UtcNow;
        UpdatedAt   =  DateTime.UtcNow;
    }
    #endregion
}
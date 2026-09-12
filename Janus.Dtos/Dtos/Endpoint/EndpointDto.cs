using Janus.Domain.Enums;

namespace Janus.Dtos.Dtos.Endpoint;

public class EndpointDto
{
    public string ClientName   { get; set; } = string.Empty;
    public string ClientRoute  { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the trusted absolute URL used as the forwarding destination.
    /// </summary>
    public string TargetUrl    { get; set; } = string.Empty;
    public EHttpMethods Method { get; set; } 
    public bool Enabled        { get; set; } = true;
}

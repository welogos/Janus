using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;

namespace Janus.Application.Interfaces.Services.Endpoint;

public interface IEndpointService
{
    Task CreateEndpointAsync(EndpointDto dto,  CancellationToken cancellationToken);
    Task DeleteEndpointAsync(Guid id,  CancellationToken cancellationToken);
    Task<EndpointDomain> GetEndpointAsync(Guid id,  CancellationToken cancellationToken);
    Task<List<EndpointDomain>> GetEndpointsAsync(CancellationToken cancellationToken);
}
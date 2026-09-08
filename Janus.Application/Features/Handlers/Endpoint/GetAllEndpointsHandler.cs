using Janus.Application.Features.CQRS.Queries.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Models;
using MediatR;

namespace Janus.Application.Features.Handlers.Endpoint;

public class GetAllEndpointsHandler(IEndpointService service) : IRequestHandler<GetAllEndpointsQuery, List<EndpointDomain>>
{
    public Task<List<EndpointDomain>> Handle(GetAllEndpointsQuery request, CancellationToken cancellationToken)
        => service.GetEndpointsAsync(cancellationToken);
}
using Janus.Application.Features.CQRS.Queries.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Models;
using MediatR;

namespace Janus.Application.Features.Handlers.Endpoint;

public class GetEndpointHandler(IEndpointService service) : IRequestHandler<GetEndpointQuery, EndpointDomain>
{
    public Task<EndpointDomain> Handle(GetEndpointQuery request, CancellationToken cancellationToken)
        => service.GetEndpointAsync(request.Id, cancellationToken);
}
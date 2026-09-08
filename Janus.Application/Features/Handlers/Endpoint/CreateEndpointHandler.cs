using Janus.Application.Features.CQRS.Commands.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using MediatR;

namespace Janus.Application.Features.Handlers.Endpoint;

public class CreateEndpointHandler(IEndpointService service) : IRequestHandler<CreateEndpointCommand>
{
    public Task Handle(CreateEndpointCommand request, CancellationToken cancellationToken)
        => service.CreateEndpointAsync(request.Dto, cancellationToken);
}
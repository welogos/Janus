using Janus.Application.Features.CQRS.Commands.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using MediatR;

namespace Janus.Application.Features.Handlers.Endpoint;

public class DeleteEndpointHandler(IEndpointService service) : IRequestHandler<DeleteEndpointCommand>
{
    public Task Handle(DeleteEndpointCommand request, CancellationToken cancellationToken)
        => service.DeleteEndpointAsync(request.id, cancellationToken);
}
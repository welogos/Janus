using MediatR;

namespace Janus.Application.Features.CQRS.Commands.Endpoint;

public record DeleteEndpointCommand(Guid id) : IRequest;
using Janus.Dtos.Dtos.Endpoint;
using MediatR;

namespace Janus.Application.Features.CQRS.Commands.Endpoint;

public record CreateEndpointCommand(EndpointDto Dto) : IRequest;
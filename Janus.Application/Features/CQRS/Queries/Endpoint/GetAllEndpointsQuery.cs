using Janus.Domain.Models;
using MediatR;

namespace Janus.Application.Features.CQRS.Queries.Endpoint;

public record GetAllEndpointsQuery : IRequest<List<EndpointDomain>>;
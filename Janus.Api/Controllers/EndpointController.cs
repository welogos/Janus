using Janus.Application.Features.CQRS.Commands.Endpoint;
using Janus.Application.Features.CQRS.Queries.Endpoint;
using Janus.Dtos.Dtos.Endpoint;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Janus.Api.Controllers;

[ApiController]
[Route("endpoints")]
public class EndpointController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateEndpoint([FromBody] EndpointDto dto)
    { await mediator.Send(new CreateEndpointCommand(dto)); return Created(); }

    [HttpGet]
    public async Task<IActionResult> GetEndpoints()
        => Ok((await mediator.Send(new GetAllEndpointsQuery()))
            .Select(EndpointResponseDto.FromDomain));
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEndpoint(Guid id)
        => Ok(EndpointResponseDto.FromDomain(
            await mediator.Send(new GetEndpointQuery(id))));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEndpoint(Guid id)
    { await mediator.Send(new DeleteEndpointCommand(id)); return NoContent(); }
}

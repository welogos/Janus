using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;
using Janus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Janus.Infrastructure.Services.Endpoints;

public class EndpointService(AppDbContext context) : IEndpointService
{
    public async Task CreateEndpointAsync(EndpointDto dto, CancellationToken cancellationToken)
    {
        var endpoint = new EndpointDomain(dto.ClientName, dto.ClientRoute, dto.Method, dto.Enabled);
        
        await context.Endpoints.AddAsync(endpoint, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteEndpointAsync(Guid id, CancellationToken cancellationToken)
    {
        var endpoint = await context.Endpoints.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Endpoint not found.");
        
        context.Endpoints.Remove(endpoint);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EndpointDomain> GetEndpointAsync(Guid id, CancellationToken cancellationToken)
        => await context.Endpoints.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Endpoint not found.");

    public Task<List<EndpointDomain>> GetEndpointsAsync(CancellationToken cancellationToken)
        => context.Endpoints.ToListAsync(cancellationToken);
}
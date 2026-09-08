using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;
using Janus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Janus.Infrastructure.Services.Endpoints;

public class EndpointService(AppDbContext context, IEndpointRegistry registry, ILogger<EndpointService> logger) : IEndpointService
{
    public async Task CreateEndpointAsync(EndpointDto dto, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Creating endpoint for client {ClientName} with route {ClientRoute} and method {Method}.",
            dto.ClientName,
            dto.ClientRoute,
            dto.Method);

        if (await context.Endpoints.AnyAsync(x => x.ClientRoute == dto.ClientRoute && x.Method == dto.Method, cancellationToken))
            throw new ArgumentException("Endpoint already exists.");
        
        
        var endpoint = new EndpointDomain(dto.ClientName, dto.ClientRoute, dto.Method, dto.Enabled);

        await context.Endpoints.AddAsync(endpoint, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await registry.SetEndpointAsync(endpoint);

        logger.LogInformation(
            "Endpoint {EndpointId} created for client {ClientName}.",
            endpoint.Id,
            endpoint.ClientName);
    }

    public async Task DeleteEndpointAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting endpoint {EndpointId}.", id);

        var endpoint = await context.Endpoints.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Endpoint with id '{id}' was not found.");
        
        context.Endpoints.Remove(endpoint);
        await context.SaveChangesAsync(cancellationToken);
        await registry.RemoveEndpointAsync(id);

        logger.LogInformation("Endpoint {EndpointId} deleted.", id);
    }

    public async Task<EndpointDomain> GetEndpointAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Retrieving endpoint {EndpointId}.", id);

        var endpoint = await registry.GetEndpointAsync(id);

        if (endpoint is not null)
        {
            logger.LogDebug(
                "Endpoint {EndpointId} retrieved from the endpoint registry.",
                id);

            return endpoint;
        }

        logger.LogDebug(
            "Endpoint {EndpointId} was not present in the endpoint registry; querying the database.",
            id);

        endpoint = await context.Endpoints.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Endpoint with id '{id}' was not found.");

        await registry.SetEndpointAsync(endpoint);

        return endpoint;
    }

    public async Task<List<EndpointDomain>> GetEndpointsAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Retrieving endpoints.");

        var endpoints = await registry.GetEndpointsAsync();

        if (endpoints is not null)
        {
            logger.LogDebug(
                "Retrieved {EndpointCount} endpoints from the endpoint registry.",
                endpoints.Count);

            return endpoints;
        }

        logger.LogDebug(
            "The endpoint registry has not been initialized; querying enabled endpoints from the database.");

        endpoints = await context.Endpoints.Where(x => x.Enabled).ToListAsync(cancellationToken);
        await registry.LoadEndpointsAsync(endpoints);

        logger.LogInformation(
            "Loaded {EndpointCount} enabled endpoints from the database.",
            endpoints.Count);

        return endpoints;
    }
}

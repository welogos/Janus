using Janus.Application.Features.CQRS.Commands.Endpoint;
using Janus.Application.Features.CQRS.Queries.Endpoint;
using Janus.Application.Features.Handlers.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;

namespace Janus.Application.Tests.Handlers;

public class EndpointHandlerTests
{
    #region Fact Tests
    [Fact]
    public async Task WhenCreateEndpointIsHandledHasForwardedRequest()
    {
        var service = new EndpointServiceFake();
        var handler = new CreateEndpointHandler(service);
        var endpointDto = CreateEndpointDto();
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.Handle(
            new CreateEndpointCommand(endpointDto),
            cancellationTokenSource.Token);

        Assert.Same(endpointDto, service.CreatedEndpointDto);
        Assert.Equal(cancellationTokenSource.Token, service.CancellationToken);
    }

    [Fact]
    public async Task WhenDeleteEndpointIsHandledHasForwardedRequest()
    {
        var service = new EndpointServiceFake();
        var handler = new DeleteEndpointHandler(service);
        var id = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.Handle(
            new DeleteEndpointCommand(id),
            cancellationTokenSource.Token);

        Assert.Equal(id, service.DeletedEndpointId);
        Assert.Equal(cancellationTokenSource.Token, service.CancellationToken);
    }

    [Fact]
    public async Task WhenGetEndpointIsHandledHasReturnedServiceResult()
    {
        var endpoint = new EndpointDomain("Client", "/route", EHttpMethods.Get);
        var service = new EndpointServiceFake { Endpoint = endpoint };
        var handler = new GetEndpointHandler(service);
        var id = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new GetEndpointQuery(id),
            cancellationTokenSource.Token);

        Assert.Same(endpoint, result);
        Assert.Equal(id, service.RequestedEndpointId);
        Assert.Equal(cancellationTokenSource.Token, service.CancellationToken);
    }

    [Fact]
    public async Task WhenGetAllEndpointsIsHandledHasReturnedServiceResult()
    {
        var endpoints = new List<EndpointDomain>
        {
            new("Client A", "/route-a", EHttpMethods.Get),
            new("Client B", "/route-b", EHttpMethods.Post),
        };
        var service = new EndpointServiceFake { Endpoints = endpoints };
        var handler = new GetAllEndpointsHandler(service);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new GetAllEndpointsQuery(),
            cancellationTokenSource.Token);

        Assert.Same(endpoints, result);
        Assert.Equal(cancellationTokenSource.Token, service.CancellationToken);
    }
    #endregion

    #region Private Methods
    private static EndpointDto CreateEndpointDto()
        => new()
        {
            ClientName = "Client",
            ClientRoute = "/route",
            Method = EHttpMethods.Post,
            Enabled = true,
        };
    #endregion

    #region Fakes
    private sealed class EndpointServiceFake : IEndpointService
    {
        public EndpointDto? CreatedEndpointDto { get; private set; }
        public Guid? DeletedEndpointId { get; private set; }
        public Guid? RequestedEndpointId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public EndpointDomain Endpoint { get; init; } = null!;
        public List<EndpointDomain> Endpoints { get; init; } = [];

        public Task CreateEndpointAsync(EndpointDto dto, CancellationToken cancellationToken)
        {
            CreatedEndpointDto = dto;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task DeleteEndpointAsync(Guid id, CancellationToken cancellationToken)
        {
            DeletedEndpointId = id;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<EndpointDomain> GetEndpointAsync(Guid id, CancellationToken cancellationToken)
        {
            RequestedEndpointId = id;
            CancellationToken = cancellationToken;
            return Task.FromResult(Endpoint);
        }

        public Task<List<EndpointDomain>> GetEndpointsAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(Endpoints);
        }
    }
    #endregion
}

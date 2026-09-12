using Janus.Api.Controllers;
using Janus.Application.Features.Handlers.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.Api.Tests.Controllers;

public class EndpointControllerTests
{
    #region Fact Tests
    [Fact]
    public async Task WhenEndpointIsCreatedHasReturnedCreatedResult()
    {
        var service = new EndpointServiceFake();
        using var serviceProvider = CreateServiceProvider(service);
        var controller = CreateController(serviceProvider);
        var endpointDto = CreateEndpointDto();

        var result = await controller.CreateEndpoint(endpointDto);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Same(endpointDto, service.CreatedEndpointDto);
    }

    [Fact]
    public async Task WhenEndpointsAreRequestedHasReturnedOkWithEndpoints()
    {
        var endpoints = new List<EndpointDomain>
        {
            new("Client A", "/route-a", EHttpMethods.Get),
            new("Client B", "/route-b", EHttpMethods.Post),
        };
        var service = new EndpointServiceFake { Endpoints = endpoints };
        using var serviceProvider = CreateServiceProvider(service);
        var controller = CreateController(serviceProvider);

        var result = await controller.GetEndpoints();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<EndpointResponseDto>>(okResult.Value);
        Assert.Equal(endpoints.Select(endpoint => endpoint.Id), response.Select(endpoint => endpoint.Id));
    }

    [Fact]
    public async Task WhenEndpointIsRequestedHasReturnedOkWithEndpoint()
    {
        var endpoint = new EndpointDomain("Client", "/route", EHttpMethods.Get);
        var service = new EndpointServiceFake { Endpoint = endpoint };
        using var serviceProvider = CreateServiceProvider(service);
        var controller = CreateController(serviceProvider);

        var result = await controller.GetEndpoint(endpoint.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EndpointResponseDto>(okResult.Value);
        Assert.Equal(endpoint.Id, response.Id);
        Assert.DoesNotContain(
            typeof(EndpointResponseDto).GetProperties(),
            property => property.Name == nameof(EndpointDomain.TargetUrl));
        Assert.Equal(endpoint.Id, service.RequestedEndpointId);
    }

    [Fact]
    public async Task WhenEndpointIsDeletedHasReturnedNoContentResult()
    {
        var service = new EndpointServiceFake();
        using var serviceProvider = CreateServiceProvider(service);
        var controller = CreateController(serviceProvider);
        var id = Guid.NewGuid();

        var result = await controller.DeleteEndpoint(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, service.DeletedEndpointId);
    }
    #endregion

    #region Private Methods
    private static ServiceProvider CreateServiceProvider(IEndpointService endpointService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(endpointService);
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(CreateEndpointHandler).Assembly));

        return services.BuildServiceProvider();
    }

    private static EndpointController CreateController(IServiceProvider serviceProvider)
        => new(serviceProvider.GetRequiredService<IMediator>());

    private static EndpointDto CreateEndpointDto()
        => new()
        {
            ClientName = "Portfolio",
            ClientRoute = "/api/chat",
            TargetUrl = "http://hermes:8080/api/chat",
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
        public EndpointDomain Endpoint { get; init; } = null!;
        public List<EndpointDomain> Endpoints { get; init; } = [];

        public Task CreateEndpointAsync(EndpointDto dto, CancellationToken cancellationToken)
        {
            CreatedEndpointDto = dto;
            return Task.CompletedTask;
        }

        public Task DeleteEndpointAsync(Guid id, CancellationToken cancellationToken)
        {
            DeletedEndpointId = id;
            return Task.CompletedTask;
        }

        public Task<EndpointDomain> GetEndpointAsync(Guid id, CancellationToken cancellationToken)
        {
            RequestedEndpointId = id;
            return Task.FromResult(Endpoint);
        }

        public Task<List<EndpointDomain>> GetEndpointsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Endpoints);
    }
    #endregion
}

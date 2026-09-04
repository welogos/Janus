using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Dtos.Dtos.Endpoint;
using Janus.Infrastructure.Context;
using Janus.Infrastructure.Services.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace Janus.Infrastructure.Tests.Services;

public class EndpointServiceTests
{
    #region Fact Tests
    [Fact]
    public async Task WhenEndpointIsCreatedHasPersistedProvidedValues()
    {
        await using var context = CreateContext();
        var service = new EndpointService(context);
        var endpointDto = CreateEndpointDto(enabled: false);

        await service.CreateEndpointAsync(endpointDto, CancellationToken.None);
        context.ChangeTracker.Clear();

        var endpoint = await context.Endpoints.SingleAsync();

        Assert.NotEqual(Guid.Empty, endpoint.Id);
        Assert.Equal(endpointDto.ClientName, endpoint.ClientName);
        Assert.Equal(endpointDto.ClientRoute, endpoint.ClientRoute);
        Assert.Equal(endpointDto.Method, endpoint.Method);
        Assert.False(endpoint.Enabled);
    }

    [Fact]
    public async Task WhenEndpointsExistHasReturnedAllEndpoints()
    {
        await using var context = CreateContext();
        var firstEndpoint = new EndpointDomain("Client A", "/route-a", EHttpMethods.Get);
        var secondEndpoint = new EndpointDomain("Client B", "/route-b", EHttpMethods.Post);
        context.Endpoints.AddRange(firstEndpoint, secondEndpoint);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new EndpointService(context);

        var result = await service.GetEndpointsAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, endpoint => endpoint.Id == firstEndpoint.Id);
        Assert.Contains(result, endpoint => endpoint.Id == secondEndpoint.Id);
    }

    [Fact]
    public async Task WhenNoEndpointExistsHasReturnedEmptyList()
    {
        await using var context = CreateContext();
        var service = new EndpointService(context);

        var result = await service.GetEndpointsAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task WhenEndpointExistsHasReturnedEndpoint()
    {
        await using var context = CreateContext();
        var endpoint = new EndpointDomain("Client", "/route", EHttpMethods.Get);
        context.Endpoints.Add(endpoint);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new EndpointService(context);

        var result = await service.GetEndpointAsync(endpoint.Id, CancellationToken.None);

        Assert.Equal(endpoint.Id, result.Id);
        Assert.Equal(endpoint.ClientName, result.ClientName);
    }

    [Fact]
    public async Task WhenEndpointDoesNotExistHasThrownKeyNotFoundException()
    {
        await using var context = CreateContext();
        var service = new EndpointService(context);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetEndpointAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("Endpoint not found.", exception.Message);
    }

    [Fact]
    public async Task WhenEndpointIsDeletedHasRemovedEndpoint()
    {
        await using var context = CreateContext();
        var endpoint = new EndpointDomain("Client", "/route", EHttpMethods.Delete);
        context.Endpoints.Add(endpoint);
        await context.SaveChangesAsync();
        var service = new EndpointService(context);

        await service.DeleteEndpointAsync(endpoint.Id, CancellationToken.None);

        Assert.False(await context.Endpoints.AnyAsync(x => x.Id == endpoint.Id));
    }

    [Fact]
    public async Task WhenDeletingEndpointThatDoesNotExistHasThrownKeyNotFoundException()
    {
        await using var context = CreateContext();
        var service = new EndpointService(context);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteEndpointAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("Endpoint not found.", exception.Message);
    }
    #endregion

    #region Private Methods
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static EndpointDto CreateEndpointDto(bool enabled = true)
        => new()
        {
            ClientName = "Portfolio",
            ClientRoute = "/api/chat",
            Method = EHttpMethods.Post,
            Enabled = enabled,
        };
    #endregion
}

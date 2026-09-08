using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Infrastructure.Services.Endpoints;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Janus.Infrastructure.Tests.Services;

public class EndpointRegistryTests
{
    [Fact]
    public async Task WhenRegistryHasNotBeenLoadedHasReturnedNullEndpoints()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);

        var endpoints = await registry.GetEndpointsAsync();

        Assert.Null(endpoints);
    }

    [Fact]
    public async Task WhenEndpointsAreLoadedHasStoredOnlyEnabledEndpoints()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var enabledEndpoint = CreateEndpoint("/enabled", enabled: true);
        var disabledEndpoint = CreateEndpoint("/disabled", enabled: false);

        await registry.LoadEndpointsAsync([enabledEndpoint, disabledEndpoint]);

        var endpoints = await registry.GetEndpointsAsync();

        var endpoint = Assert.Single(Assert.IsType<List<EndpointDomain>>(endpoints));
        Assert.Equal(enabledEndpoint.Id, endpoint.Id);
    }

    [Fact]
    public async Task WhenEndpointExistsHasReturnedEndpointById()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var endpoint = CreateEndpoint("/endpoint");
        await registry.LoadEndpointsAsync([endpoint]);

        var result = await registry.GetEndpointAsync(endpoint.Id);

        Assert.Same(endpoint, result);
    }

    [Fact]
    public async Task WhenEndpointDoesNotExistHasReturnedNullById()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        await registry.LoadEndpointsAsync([]);

        var result = await registry.GetEndpointAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task WhenRouteAndMethodMatchHasResolvedEndpoint()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var endpoint = CreateEndpoint("/portfolio/test", EHttpMethods.Post);
        await registry.LoadEndpointsAsync([endpoint]);

        var result = await registry.FindAsync("/portfolio/test", EHttpMethods.Post);

        Assert.Same(endpoint, result);
    }

    [Theory]
    [InlineData("portfolio/test")]
    [InlineData(" /PORTFOLIO/TEST ")]
    [InlineData("/portfolio/test/")]
    public async Task WhenRouteFormattingDiffersHasResolvedNormalizedRoute(string route)
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var endpoint = CreateEndpoint("/portfolio/test", EHttpMethods.Get);
        await registry.LoadEndpointsAsync([endpoint]);

        var result = await registry.FindAsync(route, EHttpMethods.Get);

        Assert.Same(endpoint, result);
    }

    [Fact]
    public async Task WhenEndpointsShareRouteHasResolvedEndpointByMethod()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var getEndpoint = CreateEndpoint("/shared", EHttpMethods.Get);
        var postEndpoint = CreateEndpoint("/shared", EHttpMethods.Post);
        await registry.LoadEndpointsAsync([getEndpoint, postEndpoint]);

        var getResult = await registry.FindAsync("/shared", EHttpMethods.Get);
        var postResult = await registry.FindAsync("/shared", EHttpMethods.Post);

        Assert.Same(getEndpoint, getResult);
        Assert.Same(postEndpoint, postResult);
    }

    [Fact]
    public async Task WhenRouteExistsWithDifferentMethodHasReturnedNull()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        await registry.LoadEndpointsAsync(
            [CreateEndpoint("/portfolio/test", EHttpMethods.Get)]);

        var result = await registry.FindAsync("/portfolio/test", EHttpMethods.Post);

        Assert.Null(result);
    }

    [Fact]
    public async Task WhenEnabledEndpointIsSetHasAddedEndpoint()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var endpoint = CreateEndpoint("/new");
        await registry.LoadEndpointsAsync([]);

        await registry.SetEndpointAsync(endpoint);

        var endpoints = await registry.GetEndpointsAsync();
        Assert.Same(endpoint, Assert.Single(Assert.IsType<List<EndpointDomain>>(endpoints)));
    }

    [Fact]
    public async Task WhenSameEndpointIsSetAgainHasNotCreatedDuplicate()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var endpoint = CreateEndpoint("/existing");
        await registry.LoadEndpointsAsync([endpoint]);

        await registry.SetEndpointAsync(endpoint);

        var endpoints = await registry.GetEndpointsAsync();
        Assert.Same(endpoint, Assert.Single(Assert.IsType<List<EndpointDomain>>(endpoints)));
    }

    [Fact]
    public async Task WhenDisabledEndpointIsSetHasNotAddedEndpoint()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        await registry.LoadEndpointsAsync([]);

        await registry.SetEndpointAsync(CreateEndpoint("/disabled", enabled: false));

        Assert.Empty(Assert.IsType<List<EndpointDomain>>(
            await registry.GetEndpointsAsync()));
    }

    [Fact]
    public async Task WhenExistingEndpointIsRemovedHasRemovedEndpoint()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var endpoint = CreateEndpoint("/existing");
        await registry.LoadEndpointsAsync([endpoint]);

        await registry.RemoveEndpointAsync(endpoint.Id);

        Assert.Empty(Assert.IsType<List<EndpointDomain>>(
            await registry.GetEndpointsAsync()));
    }

    [Fact]
    public async Task WhenMissingEndpointIsRemovedHasThrownKeyNotFoundException()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);
        var id = Guid.NewGuid();
        await registry.LoadEndpointsAsync([]);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => registry.RemoveEndpointAsync(id));

        Assert.Equal(
            $"Endpoint with id '{id}' was not found in the endpoint registry.",
            exception.Message);
    }

    [Fact]
    public async Task WhenUninitializedRegistryIsUpdatedHasThrownInvalidOperationException()
    {
        using var cache = CreateCache();
        var registry = CreateRegistry(cache);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.SetEndpointAsync(CreateEndpoint("/new")));

        Assert.Equal(
            "The endpoint registry has not been initialized.",
            exception.Message);
    }

    private static MemoryCache CreateCache()
        => new(new MemoryCacheOptions());

    private static EndpointRegistry CreateRegistry(IMemoryCache cache)
        => new(cache, NullLogger<EndpointRegistry>.Instance);

    private static EndpointDomain CreateEndpoint(
        string route,
        EHttpMethods method = EHttpMethods.Get,
        bool enabled = true)
        => new("Client", route, method, enabled);
}

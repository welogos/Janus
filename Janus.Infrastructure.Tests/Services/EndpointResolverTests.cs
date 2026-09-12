using Janus.Application.Models.Dispatching;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Infrastructure.Services.Dispatching;
using Janus.Infrastructure.Services.Endpoints;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Janus.Infrastructure.Tests.Services;

public class EndpointResolverTests
{
    [Fact]
    public async Task WhenEnabledEndpointMatchesHasResolvedEndpoint()
    {
        var endpoint = CreateEndpoint("/orders", EHttpMethods.Post);
        var resolver = await CreateResolverAsync(endpoint);

        var result = await resolver.ResolveAsync("/orders/", "POST");

        Assert.Equal(EndpointResolutionStatus.Found, result.Status);
        Assert.Same(endpoint, result.Endpoint);
    }

    [Fact]
    public async Task WhenRouteDoesNotExistHasReturnedNotFound()
    {
        var resolver = await CreateResolverAsync();

        var result = await resolver.ResolveAsync("/missing", "GET");

        Assert.Equal(EndpointResolutionStatus.NotFound, result.Status);
        Assert.Null(result.Endpoint);
    }

    [Fact]
    public async Task WhenEndpointIsDisabledHasReturnedNotFound()
    {
        var resolver = await CreateResolverAsync(
            CreateEndpoint("/disabled", EHttpMethods.Get, enabled: false));

        var result = await resolver.ResolveAsync("/disabled", "GET");

        Assert.Equal(EndpointResolutionStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task WhenRouteExistsForOtherMethodsHasReturnedMethodNotAllowedAndMethods()
    {
        var resolver = await CreateResolverAsync(
            CreateEndpoint("/orders", EHttpMethods.Get),
            CreateEndpoint("/orders", EHttpMethods.Delete));

        var result = await resolver.ResolveAsync("/orders", "POST");

        Assert.Equal(EndpointResolutionStatus.MethodNotAllowed, result.Status);
        Assert.Equal([EHttpMethods.Get, EHttpMethods.Delete], result.AllowedMethods);
    }

    private static async Task<EndpointResolver> CreateResolverAsync(
        params EndpointDomain[] endpoints)
    {
        var registry = new EndpointRegistry(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<EndpointRegistry>.Instance);
        await registry.LoadEndpointsAsync([.. endpoints]);
        return new EndpointResolver(registry, NullLogger<EndpointResolver>.Instance);
    }

    private static EndpointDomain CreateEndpoint(
        string route,
        EHttpMethods method,
        bool enabled = true)
        => new(
            "Hermes",
            route,
            method,
            enabled,
            "http://hermes:8080/internal");
}

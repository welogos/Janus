using System.Text;
using Janus.Api.Extensions;
using Janus.Application.Interfaces.Services.Dispatching;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Application.Models.Dispatching;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Infrastructure.Services.Dispatching;
using Janus.Infrastructure.Services.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.Api.Tests.Routing;

public class DynamicEndpointFallbackTests
{
    [Fact]
    public async Task WhenPostEndpointIsRegisteredHasDispatchedRequestAndReturnedResponse()
    {
        var endpoint = CreateEndpoint("/portfolio/test", EHttpMethods.Post);
        var context = CreateHttpContext(HttpMethods.Post, "/portfolio/test?lang=en");
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"message":"hello"}"""));
        context.Request.ContentLength = context.Request.Body.Length;
        var dispatcher = new EndpointDispatcherFake(
            new DispatchResponse(
                StatusCodes.Status202Accepted,
                Encoding.UTF8.GetBytes("""{"accepted":true}"""),
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["Content-Type"] = ["application/json"],
                }));
        await using var application = await CreateApplicationAsync(dispatcher, endpoint);

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("?lang=en", dispatcher.Request!.QueryString);
        Assert.Equal("""{"message":"hello"}""", Encoding.UTF8.GetString(dispatcher.Request.Body.Span));
        Assert.Equal("""{"accepted":true}""", await ReadResponseBodyAsync(context));
    }

    [Fact]
    public async Task WhenDynamicRouteDoesNotExistHasReturnedNotFound()
    {
        var context = CreateHttpContext(HttpMethods.Get, "/missing");
        var dispatcher = new EndpointDispatcherFake();
        await using var application = await CreateApplicationAsync(dispatcher);

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Null(dispatcher.Request);
    }

    [Fact]
    public async Task WhenEndpointIsDisabledHasReturnedNotFound()
    {
        var context = CreateHttpContext(HttpMethods.Get, "/disabled");
        var dispatcher = new EndpointDispatcherFake();
        await using var application = await CreateApplicationAsync(
            dispatcher,
            CreateEndpoint("/disabled", enabled: false));

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Null(dispatcher.Request);
    }

    [Fact]
    public async Task WhenRouteExistsForDifferentMethodHasReturnedMethodNotAllowedWithAllowHeader()
    {
        var context = CreateHttpContext(HttpMethods.Post, "/portfolio/test");
        var dispatcher = new EndpointDispatcherFake();
        await using var application = await CreateApplicationAsync(
            dispatcher,
            CreateEndpoint("/portfolio/test", EHttpMethods.Get));

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
        Assert.Equal("GET", context.Response.Headers.Allow);
        Assert.Null(dispatcher.Request);
    }

    [Fact]
    public async Task WhenHttpMethodIsUnsupportedForExistingRouteHasReturnedMethodNotAllowed()
    {
        var context = CreateHttpContext(HttpMethods.Patch, "/portfolio/test");
        var dispatcher = new EndpointDispatcherFake();
        await using var application = await CreateApplicationAsync(
            dispatcher,
            CreateEndpoint("/portfolio/test", EHttpMethods.Get));

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
        Assert.Equal("GET", context.Response.Headers.Allow);
    }

    [Fact]
    public async Task WhenDestinationReturnsNoContentHasPreservedStatusAndEmptyBody()
    {
        var context = CreateHttpContext(HttpMethods.Delete, "/resource");
        var dispatcher = new EndpointDispatcherFake(
            new DispatchResponse(
                StatusCodes.Status204NoContent,
                ReadOnlyMemory<byte>.Empty,
                new Dictionary<string, IReadOnlyList<string>>()));
        await using var application = await CreateApplicationAsync(
            dispatcher,
            CreateEndpoint("/resource", EHttpMethods.Delete));

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static async Task<WebApplication> CreateApplicationAsync(
        IEndpointDispatcher dispatcher,
        params EndpointDomain[] endpoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IEndpointRegistry, EndpointRegistry>();
        builder.Services.AddSingleton<IEndpointResolver, EndpointResolver>();
        builder.Services.AddSingleton(dispatcher);

        var application = builder.Build();
        var registry = application.Services.GetRequiredService<IEndpointRegistry>();

        await registry.LoadEndpointsAsync([.. endpoints]);
        await application.EndpointFallBackAsync();

        return application;
    }

    private static DefaultHttpContext CreateHttpContext(string method, string pathAndQuery)
    {
        var context = new DefaultHttpContext();
        var separator = pathAndQuery.IndexOf('?');
        context.Request.Method = method;
        context.Request.Path = separator < 0 ? pathAndQuery : pathAndQuery[..separator];
        context.Request.QueryString = separator < 0
            ? QueryString.Empty
            : new QueryString(pathAndQuery[separator..]);
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-correlation-id";
        return context;
    }

    private static async Task ExecuteFallbackAsync(
        WebApplication application,
        HttpContext context)
    {
        context.RequestServices = application.Services;
        var fallback = ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/{**path}");

        await fallback.RequestDelegate!(context);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static EndpointDomain CreateEndpoint(
        string route,
        EHttpMethods method = EHttpMethods.Get,
        bool enabled = true)
        => new(
            "Hermes",
            route,
            method,
            enabled,
            "http://hermes:8080/internal");

    private sealed class EndpointDispatcherFake(
        DispatchResponse? response = null) : IEndpointDispatcher
    {
        public DispatchRequest? Request { get; private set; }

        public Task<DispatchResponse> DispatchAsync(
            EndpointDomain endpoint,
            DispatchRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(response ?? new DispatchResponse(
                StatusCodes.Status200OK,
                ReadOnlyMemory<byte>.Empty,
                new Dictionary<string, IReadOnlyList<string>>()));
        }
    }
}

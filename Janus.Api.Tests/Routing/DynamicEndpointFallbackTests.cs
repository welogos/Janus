using System.Text.Json;
using Janus.Api.Extensions;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Infrastructure.Services.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.Api.Tests.Routing;

public class DynamicEndpointFallbackTests
{
    [Fact]
    public async Task WhenPostEndpointIsRegisteredHasResolvedDynamicEndpoint()
    {
        var endpoint = new EndpointDomain(
            "Portfolio",
            "/portfolio/test",
            EHttpMethods.Post);
        var context = CreateHttpContext(HttpMethods.Post, "/portfolio/test");
        await using var application = await CreateApplicationAsync(context, endpoint);

        await ExecuteFallbackAsync(application, context);

        using var response = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(
            endpoint.Id,
            response.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(
            endpoint.ClientRoute,
            response.RootElement.GetProperty("clientRoute").GetString());
    }

    [Fact]
    public async Task WhenDynamicRouteDoesNotExistHasReturnedNotFound()
    {
        var context = CreateHttpContext(HttpMethods.Get, "/missing");
        await using var application = await CreateApplicationAsync(context);

        await ExecuteFallbackAsync(application, context);

        using var response = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(
            "Dynamic endpoint was not found.",
            response.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task WhenRouteExistsForDifferentMethodHasReturnedNotFound()
    {
        var endpoint = new EndpointDomain(
            "Portfolio",
            "/portfolio/test",
            EHttpMethods.Get);
        var context = CreateHttpContext(HttpMethods.Post, "/portfolio/test");
        await using var application = await CreateApplicationAsync(context, endpoint);

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task WhenHttpMethodIsUnsupportedHasReturnedMethodNotAllowed()
    {
        var context = CreateHttpContext(HttpMethods.Patch, "/portfolio/test");
        await using var application = await CreateApplicationAsync(context);

        await ExecuteFallbackAsync(application, context);

        Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static async Task<WebApplication> CreateApplicationAsync(
        DefaultHttpContext context,
        params EndpointDomain[] endpoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<HttpContext>(context);
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IEndpointRegistry, EndpointRegistry>();

        var application = builder.Build();
        var registry = application.Services.GetRequiredService<IEndpointRegistry>();

        await registry.LoadEndpointsAsync([.. endpoints]);
        await application.EndpointFallBackAsync();

        return application;
    }

    private static DefaultHttpContext CreateHttpContext(
        string method,
        string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task ExecuteFallbackAsync(
        WebApplication application,
        HttpContext context)
    {
        var routeBuilder = (IEndpointRouteBuilder)application;
        var fallback = routeBuilder.DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/{**path}");

        await fallback.RequestDelegate!(context);
    }

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}

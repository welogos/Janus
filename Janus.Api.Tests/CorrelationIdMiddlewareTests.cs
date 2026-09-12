using Janus.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Janus.Api.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task WhenValidCorrelationIdIsReceivedHasReusedIt()
    {
        const string correlationId = "request-123.valid";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, context.TraceIdentifier);
        Assert.Equal(
            correlationId,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains spaces")]
    [InlineData("contains\nnewline")]
    public async Task WhenCorrelationIdIsMissingOrInvalidHasGeneratedSafeIdentifier(
        string correlationId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.NotEqual(correlationId, context.TraceIdentifier);
        Assert.True(Guid.TryParseExact(context.TraceIdentifier, "N", out _));
        Assert.Equal(
            context.TraceIdentifier,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task WhenMultipleCorrelationIdsAreReceivedHasGeneratedNewIdentifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] =
            new Microsoft.Extensions.Primitives.StringValues(["first", "second"]);
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.NotEqual("first", context.TraceIdentifier);
        Assert.NotEqual("second", context.TraceIdentifier);
        Assert.True(Guid.TryParseExact(context.TraceIdentifier, "N", out _));
    }

    private static CorrelationIdMiddleware CreateMiddleware()
        => new(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);
}

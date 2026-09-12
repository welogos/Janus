using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Janus.Api.ExceptionHandling;
using Janus.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Janus.Api.Tests.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> KnownExceptions => new()
    {
        {
            new KeyNotFoundException("Endpoint with id 'test-id' was not found."),
            StatusCodes.Status404NotFound,
            "Resource not found"
        },
        {
            new ArgumentException("Internal argument detail."),
            StatusCodes.Status400BadRequest,
            "Invalid request"
        },
        {
            new ArgumentNullException("value"),
            StatusCodes.Status400BadRequest,
            "Invalid request"
        },
        {
            new UnauthorizedAccessException("Internal authentication detail."),
            StatusCodes.Status401Unauthorized,
            "Unauthorized"
        }
    };

    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task WhenKnownExceptionIsHandledHasReturnedExpectedProblemDetails(
        Exception exception,
        int expectedStatus,
        string expectedTitle)
    {
        var context = CreateHttpContext();
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        using var document = await ReadResponseAsync(context);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WhenValidationExceptionIsHandledHasReturnedValidationErrors()
    {
        var context = CreateHttpContext();
        var handler = CreateHandler();
        var exception = new ValidationException(
        [
            new ValidationFailure("ClientRoute", "Client route is required.")
        ]);

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        using var document = await ReadResponseAsync(context);
        var errors = document.RootElement.GetProperty("errors");

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(
            "Client route is required.",
            errors.GetProperty("ClientRoute")[0].GetString());
    }

    [Fact]
    public async Task WhenInvalidOperationExceptionIsHandledHasReturnedInternalServerError()
    {
        var context = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("The endpoint registry has not been initialized."),
            CancellationToken.None);

        using var document = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(
            "Internal server error",
            document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WhenUnexpectedExceptionIsHandledHasNotExposedSensitiveInternalDetails()
    {
        var context = CreateHttpContext();
        var handler = CreateHandler();
        var exception = CreateUnexpectedException();

        await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        using var document = await ReadResponseAsync(context);
        var responseBody = document.RootElement.GetRawText();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(
            "An unexpected error occurred while processing the request.",
            document.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain(exception.Message, responseBody);
        Assert.DoesNotContain("Password=secret", responseBody);
        Assert.DoesNotContain("Bearer secret-token", responseBody);
        Assert.DoesNotContain("stackTrace", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(GlobalExceptionHandlerTests), responseBody);
    }

    [Theory]
    [InlineData(true, StatusCodes.Status504GatewayTimeout, "Gateway timeout")]
    [InlineData(false, StatusCodes.Status502BadGateway, "Bad gateway")]
    public async Task WhenDispatchFailureIsHandledHasReturnedSafeGatewayError(
        bool timedOut,
        int expectedStatus,
        string expectedTitle)
    {
        var context = CreateHttpContext();
        context.TraceIdentifier = "correlation-123";
        var handler = CreateHandler();
        Exception exception = timedOut
            ? new EndpointDispatchTimeoutException()
            : new EndpointUpstreamException();

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        using var document = await ReadResponseAsync(context);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedTitle, document.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "correlation-123",
            document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task WhenClientCancelsHasReturnedClientClosedWithoutResponseBody()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var context = CreateHttpContext();
        context.RequestAborted = cancellationTokenSource.Token;
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(cancellationTokenSource.Token),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static GlobalExceptionHandler CreateHandler()
        => new(NullLogger<GlobalExceptionHandler>.Instance);

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/endpoints/test";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    private static Exception CreateUnexpectedException()
    {
        try
        {
            throw new Exception(
                "Database connection failed: Host=database;Password=secret; " +
                "Authorization=Bearer secret-token.");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}

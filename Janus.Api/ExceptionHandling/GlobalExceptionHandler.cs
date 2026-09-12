using FluentValidation;
using Janus.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Janus.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "Request was canceled by the client. CorrelationId={CorrelationId}, RequestMethod={RequestMethod}, RequestPath={RequestPath}.",
                httpContext.TraceIdentifier,
                httpContext.Request.Method,
                httpContext.Request.Path);

            if (!httpContext.Response.HasStarted)
                httpContext.Response.StatusCode = 499;

            return true;
        }

        var problemDetails = CreateProblemDetails(exception);
        problemDetails.Extensions["correlationId"] = httpContext.TraceIdentifier;

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "An unhandled exception occurred while processing {RequestMethod} {RequestPath}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request {RequestMethod} {RequestPath} failed with status code {StatusCode}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                problemDetails.Status);
        }

        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = exception.Message,
                Extensions =
                {
                    ["errors"] = validationException.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .Select(error => error.ErrorMessage)
                                .Distinct()
                                .ToArray())
                }
            },
            ArgumentNullException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = exception.Message
            },
            ArgumentException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = exception.Message
            },
            KeyNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = exception.Message
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = exception.Message
            },
            EndpointDispatchTimeoutException => new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Gateway timeout",
                Detail = "The destination service did not respond in time."
            },
            EndpointUpstreamException or EndpointConfigurationException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Bad gateway",
                Detail = "The configured destination service could not process the request."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An unexpected error occurred while processing the request."
            }
        };

        return problemDetails;
    }
}

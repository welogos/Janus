using System.Diagnostics;
using Janus.Application.Exceptions;
using Janus.Application.Interfaces.Services.Dispatching;
using Janus.Application.Models.Dispatching;
using Janus.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Janus.Infrastructure.Services.Dispatching;

/// <summary>
/// Dispatches dynamic endpoint requests by using clients supplied by <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class EndpointDispatcher(
    IHttpClientFactory httpClientFactory,
    ILogger<EndpointDispatcher> logger) : IEndpointDispatcher
{
    /// <summary>The named HTTP client used for endpoint dispatch.</summary>
    public const string HttpClientName = "Janus.EndpointDispatcher";

    private static readonly HashSet<string> AllowedResponseHeaders = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Cache-Control",
        "Content-Disposition",
        "Content-Language",
        "Content-Type",
        "ETag",
        "Expires",
        "Last-Modified",
        "Retry-After",
        "Vary",
    };

    /// <inheritdoc />
    public async Task<DispatchResponse> DispatchAsync(
        EndpointDomain endpoint,
        DispatchRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Starting endpoint dispatch. CorrelationId={CorrelationId}, Route={Route}, Method={Method}, EndpointId={EndpointId}, Destination={Destination}.",
            request.CorrelationId,
            request.PublicRoute,
            request.Method,
            endpoint.Id,
            endpoint.ClientName);

        try
        {
            using var outboundRequest = OutboundRequestBuilder.Build(endpoint, request);
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(
                outboundRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var headers = CopySafeResponseHeaders(response);

            logger.LogInformation(
                "Endpoint dispatch completed. CorrelationId={CorrelationId}, EndpointId={EndpointId}, StatusCode={StatusCode}, DurationMs={DurationMs}.",
                request.CorrelationId,
                endpoint.Id,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            return new DispatchResponse((int)response.StatusCode, body, headers);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Endpoint dispatch was canceled by the client. CorrelationId={CorrelationId}, EndpointId={EndpointId}, DurationMs={DurationMs}.",
                request.CorrelationId,
                endpoint.Id,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogWarning(
                exception,
                "Endpoint dispatch timed out. CorrelationId={CorrelationId}, EndpointId={EndpointId}, DurationMs={DurationMs}.",
                request.CorrelationId,
                endpoint.Id,
                stopwatch.ElapsedMilliseconds);
            throw new EndpointDispatchTimeoutException();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Endpoint dispatch connection failed. CorrelationId={CorrelationId}, EndpointId={EndpointId}, DurationMs={DurationMs}.",
                request.CorrelationId,
                endpoint.Id,
                stopwatch.ElapsedMilliseconds);
            throw new EndpointUpstreamException();
        }
        catch (IOException exception)
        {
            logger.LogWarning(
                exception,
                "Endpoint dispatch received an invalid upstream response. CorrelationId={CorrelationId}, EndpointId={EndpointId}, DurationMs={DurationMs}.",
                request.CorrelationId,
                endpoint.Id,
                stopwatch.ElapsedMilliseconds);
            throw new EndpointUpstreamException();
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CopySafeResponseHeaders(
        HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            if (AllowedResponseHeaders.Contains(header.Key))
                headers[header.Key] = header.Value.ToArray();
        }

        return headers;
    }
}

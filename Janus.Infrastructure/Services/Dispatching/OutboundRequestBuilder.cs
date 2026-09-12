using Janus.Application.Exceptions;
using Janus.Application.Models.Dispatching;
using Janus.Domain.Models;

namespace Janus.Infrastructure.Services.Dispatching;

internal static class OutboundRequestBuilder
{
    internal const string CorrelationIdHeaderName = "X-Correlation-ID";

    private static readonly HashSet<string> AllowedRequestHeaders = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "Accept-Language",
        "Content-Language",
        "User-Agent",
    };

    internal static HttpRequestMessage Build(
        EndpointDomain endpoint,
        DispatchRequest request)
    {
        if (!Uri.TryCreate(endpoint.TargetUrl, UriKind.Absolute, out var targetUri) ||
            (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(targetUri.UserInfo) ||
            !string.IsNullOrEmpty(targetUri.Fragment))
        {
            throw new EndpointConfigurationException();
        }

        var outboundRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            AppendQueryString(targetUri, request.QueryString));

        var hasContentType = request.Headers.ContainsKey("Content-Type");
        if (!request.Body.IsEmpty || hasContentType)
            outboundRequest.Content = new ByteArrayContent(request.Body.ToArray());

        foreach (var header in request.Headers)
        {
            if (!AllowedRequestHeaders.Contains(header.Key))
                continue;

            if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                outboundRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                continue;
            }

            outboundRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (outboundRequest.Content is not null &&
            request.Headers.TryGetValue("Content-Type", out var contentTypes))
        {
            outboundRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentTypes);
        }

        outboundRequest.Headers.Remove(CorrelationIdHeaderName);
        outboundRequest.Headers.TryAddWithoutValidation(
            CorrelationIdHeaderName,
            request.CorrelationId);

        return outboundRequest;
    }

    private static Uri AppendQueryString(Uri targetUri, string queryString)
    {
        var incomingQuery = queryString.TrimStart('?');
        if (string.IsNullOrEmpty(incomingQuery))
            return targetUri;

        var uriBuilder = new UriBuilder(targetUri);
        var configuredQuery = uriBuilder.Query.TrimStart('?');
        uriBuilder.Query = string.IsNullOrEmpty(configuredQuery)
            ? incomingQuery
            : $"{configuredQuery}&{incomingQuery}";

        return uriBuilder.Uri;
    }
}

using System.Diagnostics.CodeAnalysis;

namespace Janus.Api;

/// <summary>
/// Validates or creates a correlation identifier for every request and response.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>The HTTP header used for correlation identifiers.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Applies correlation metadata and invokes the next middleware.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var receivedValues = context.Request.Headers[HeaderName];
        var receivedValue = receivedValues.Count == 1
            ? receivedValues[0]
            : null;
        var correlationId = TryGetValidCorrelationId(
            receivedValue,
            out var receivedCorrelationId)
            ? receivedCorrelationId
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId,
               }))
        {
            await next(context);
        }
    }

    private static bool TryGetValidCorrelationId(
        string? value,
        [NotNullWhen(true)] out string? correlationId)
    {
        correlationId = value;

        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            return false;

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '-' or '_' or '.');
    }
}

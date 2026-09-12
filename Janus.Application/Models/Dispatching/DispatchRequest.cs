namespace Janus.Application.Models.Dispatching;

/// <summary>
/// Represents the safe, transport-neutral data required to dispatch an incoming request.
/// </summary>
/// <param name="PublicRoute">The public route received by Janus.</param>
/// <param name="Method">The incoming HTTP method.</param>
/// <param name="QueryString">The incoming query string, including its leading question mark.</param>
/// <param name="Body">The request body bytes.</param>
/// <param name="Headers">The incoming headers. The dispatcher applies its own allowlist.</param>
/// <param name="CorrelationId">The validated correlation identifier.</param>
public sealed record DispatchRequest(
    string PublicRoute,
    string Method,
    string QueryString,
    ReadOnlyMemory<byte> Body,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
    string CorrelationId);

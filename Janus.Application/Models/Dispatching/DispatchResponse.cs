namespace Janus.Application.Models.Dispatching;

/// <summary>
/// Represents the safe response data returned by a dispatched destination.
/// </summary>
/// <param name="StatusCode">The upstream HTTP status code.</param>
/// <param name="Body">The upstream response body bytes.</param>
/// <param name="Headers">The safe upstream headers that may be returned to the caller.</param>
public sealed record DispatchResponse(
    int StatusCode,
    ReadOnlyMemory<byte> Body,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers);

namespace Janus.Domain.Models;

/// <summary>
/// Provides the canonical route representation used by persisted endpoints and the registry.
/// </summary>
public static class EndpointRoute
{
    /// <summary>
    /// Normalizes a public endpoint route for deterministic comparisons.
    /// </summary>
    /// <param name="route">The route to normalize.</param>
    /// <returns>A lowercase route with one leading slash and no trailing slash, except for root.</returns>
    public static string Normalize(string route)
    {
        route = route.Trim();

        if (!route.StartsWith('/'))
            route = "/" + route;

        if (route.Length > 1)
            route = route.TrimEnd('/');

        return route.ToLowerInvariant();
    }
}

using CinemaAbyss.Proxy.Domain;
using Microsoft.Extensions.Logging;

namespace CinemaAbyss.Proxy.Application;

/// <summary>
/// Implements the Strangler Fig routing strategy.
///
/// Rules:
///   - /api/movies[/*]  → movies-service (100%) when GradualMigration = false
///   - /api/movies[/*]  → movies-service (MoviesMigrationPercent%) when GradualMigration = true
///   - everything else  → monolith (always)
/// </summary>
public sealed class TrafficRouter
{
    // Thread-local Random avoids lock contention under high concurrency.
    private static readonly ThreadLocal<Random> Rng =
        new(() => new Random(Guid.NewGuid().GetHashCode()));

    private readonly ProxyConfig _cfg;
    private readonly ILogger<TrafficRouter> _logger;

    public TrafficRouter(ProxyConfig cfg, ILogger<TrafficRouter> logger)
    {
        _cfg    = cfg;
        _logger = logger;
    }

    /// <summary>
    /// Resolves which backend should handle <paramref name="path"/>.
    /// </summary>
    /// <returns>
    /// A tuple of (backend name, target base URL).
    /// </returns>
    public (string Backend, string TargetBaseUrl) Resolve(string path)
    {
        bool isMoviesPath = path.StartsWith("/api/movies", StringComparison.OrdinalIgnoreCase);

        if (!isMoviesPath)
        {
            _logger.LogDebug("Path {Path} → monolith (non-movies route)", path);
            return (Backends.Monolith, _cfg.MonolithUrl);
        }

        bool routeToMovies = !_cfg.GradualMigration
            || Rng.Value!.Next(100) < _cfg.MoviesMigrationPercent;

        if (routeToMovies)
        {
            _logger.LogDebug("Path {Path} → movies-service (migration {Pct}%)",
                path, _cfg.GradualMigration ? _cfg.MoviesMigrationPercent : 100);
            return (Backends.Movies, _cfg.MoviesServiceUrl);
        }

        _logger.LogDebug("Path {Path} → monolith (gradual {Pct}% not hit)",
            path, _cfg.MoviesMigrationPercent);
        return (Backends.Monolith, _cfg.MonolithUrl);
    }
}

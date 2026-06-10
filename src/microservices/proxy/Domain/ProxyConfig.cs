namespace CinemaAbyss.Proxy.Domain;

/// <summary>
/// Immutable configuration snapshot for the proxy service.
/// Populated from environment variables / appsettings at startup.
/// </summary>
public sealed record ProxyConfig(
    string MonolithUrl,
    string MoviesServiceUrl,
    bool GradualMigration,
    int MoviesMigrationPercent);

using CinemaAbyss.Proxy.Application;
using CinemaAbyss.Proxy.Domain;
using CinemaAbyss.Proxy.Infrastructure;
using Serilog;
using System.Net;

// ── Composition Root ─────────────────────────────────────────────────────────

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // Domain – configuration
    builder.Services.AddSingleton<ProxyConfig>(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        return new ProxyConfig(
            MonolithUrl:             cfg["MONOLITH_URL"]             ?? "http://localhost:8080",
            MoviesServiceUrl:        cfg["MOVIES_SERVICE_URL"]        ?? "http://localhost:8081",
            GradualMigration:        bool.Parse(cfg["GRADUAL_MIGRATION"]        ?? "false"),
            MoviesMigrationPercent:  int.Parse( cfg["MOVIES_MIGRATION_PERCENT"] ?? "0")
        );
    });

    // Infrastructure – named HttpClients
    builder.Services
        .AddHttpClient(Backends.Monolith)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect      = false,
            AutomaticDecompression = DecompressionMethods.None
        });

    builder.Services
        .AddHttpClient(Backends.Movies)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect      = false,
            AutomaticDecompression = DecompressionMethods.None
        });

    // Application & Infrastructure services
    builder.Services.AddSingleton<TrafficRouter>();
    builder.Services.AddSingleton<ReverseProxyHandler>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Health check
    app.MapGet("/health", (ProxyConfig cfg) => new
    {
        status                   = "healthy",
        gradual_migration        = cfg.GradualMigration,
        movies_migration_percent = cfg.MoviesMigrationPercent,
        monolith_url             = cfg.MonolithUrl,
        movies_service_url       = cfg.MoviesServiceUrl
    });

    // Catch-all – delegate to infrastructure handler
    app.Map("/{**path}", async (HttpContext ctx, ReverseProxyHandler proxy) =>
        await proxy.HandleAsync(ctx));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Proxy service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

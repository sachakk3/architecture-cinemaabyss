using CinemaAbyss.Proxy.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;

namespace CinemaAbyss.Proxy.Infrastructure;

/// <summary>
/// Forwards an incoming HTTP request to the upstream backend resolved by
/// <see cref="TrafficRouter"/> and streams the response back to the client.
///
/// Hop-by-hop headers are stripped; forwarding headers (X-Forwarded-*)
/// are injected to let upstream services reconstruct the original request.
/// </summary>
public sealed class ReverseProxyHandler
{
    // RFC 7230 §6.1 – must not be forwarded end-to-end
    private static readonly HashSet<string> HopByHopHeaders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
            "TE", "Trailers", "Transfer-Encoding", "Upgrade", "Host"
        };

    private readonly TrafficRouter _router;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ReverseProxyHandler> _logger;

    public ReverseProxyHandler(
        TrafficRouter router,
        IHttpClientFactory factory,
        ILogger<ReverseProxyHandler> logger)
    {
        _router  = router;
        _factory = factory;
        _logger  = logger;
    }

    public async Task HandleAsync(HttpContext ctx)
    {
        var req   = ctx.Request;
        var path  = req.Path.Value  ?? "/";
        var query = req.QueryString.Value ?? string.Empty;

        var (backend, baseUrl) = _router.Resolve(path);
        var targetUri = new Uri(baseUrl.TrimEnd('/') + path + query);

        var sw = Stopwatch.StartNew();

        using var upstreamReq = BuildUpstreamRequest(req, targetUri, backend);

        try
        {
            var client = _factory.CreateClient(backend);
            using var upstreamResp = await client.SendAsync(
                upstreamReq,
                HttpCompletionOption.ResponseHeadersRead,
                ctx.RequestAborted);

            sw.Stop();
            _logger.LogInformation(
                "{Method} {Path} → [{Backend}] {StatusCode} in {Elapsed}ms",
                req.Method, path, backend,
                (int)upstreamResp.StatusCode, sw.ElapsedMilliseconds);

            await CopyResponseAsync(upstreamResp, ctx.Response, ctx.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Upstream error calling [{Backend}] {TargetUri}", backend, targetUri);

            ctx.Response.StatusCode = StatusCodes.Status502BadGateway;
            await ctx.Response.WriteAsJsonAsync(new
            {
                error   = "Bad Gateway",
                backend,
                message = ex.Message
            });
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request {Path} cancelled by client", path);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static HttpRequestMessage BuildUpstreamRequest(
        HttpRequest req, Uri targetUri, string backend)
    {
        var upstreamReq = new HttpRequestMessage
        {
            Method     = new HttpMethod(req.Method),
            RequestUri = targetUri
        };

        // Forward body
        if (req.ContentLength > 0 || req.Headers.ContainsKey("Transfer-Encoding"))
        {
            upstreamReq.Content = new StreamContent(req.Body);
            if (req.ContentType is { } ct)
                upstreamReq.Content.Headers.TryAddWithoutValidation("Content-Type", ct);
        }

        // Forward request headers (skip hop-by-hop)
        foreach (var (key, values) in req.Headers)
        {
            if (HopByHopHeaders.Contains(key)) continue;
            upstreamReq.Headers.TryAddWithoutValidation(key, values.ToArray());
        }

        // Forwarding headers
        upstreamReq.Headers.TryAddWithoutValidation(
            "X-Forwarded-For",
            req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        upstreamReq.Headers.TryAddWithoutValidation("X-Forwarded-Host",  req.Host.Value);
        upstreamReq.Headers.TryAddWithoutValidation("X-Forwarded-Proto", req.Scheme);
        upstreamReq.Headers.TryAddWithoutValidation("X-Proxy-Backend",   backend);

        return upstreamReq;
    }

    private static async Task CopyResponseAsync(
        HttpResponseMessage upstreamResp,
        HttpResponse response,
        CancellationToken ct)
    {
        response.StatusCode = (int)upstreamResp.StatusCode;

        foreach (var (key, values) in upstreamResp.Headers)
        {
            if (HopByHopHeaders.Contains(key)) continue;
            response.Headers[key] = values.ToArray();
        }

        foreach (var (key, values) in upstreamResp.Content.Headers)
            response.Headers[key] = values.ToArray();

        await upstreamResp.Content.CopyToAsync(response.Body, ct);
    }
}

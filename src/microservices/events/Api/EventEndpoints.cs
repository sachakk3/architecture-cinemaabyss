using CinemaAbyss.Events.Application;
using CinemaAbyss.Events.Domain;

namespace CinemaAbyss.Events.Api;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/events/health", () => Results.Ok(new { status = true }));

        endpoints.MapPost("/api/events/movie", async (MovieEvent payload, EventApplicationService service, CancellationToken ct) =>
        {
            try
            {
                var response = await service.PublishMovieAsync(payload, ct);
                return Results.Created("/api/events/movie", response);
            }
            catch (EventValidationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message));
            }
        });

        endpoints.MapPost("/api/events/user", async (UserEvent payload, EventApplicationService service, CancellationToken ct) =>
        {
            try
            {
                var response = await service.PublishUserAsync(payload, ct);
                return Results.Created("/api/events/user", response);
            }
            catch (EventValidationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message));
            }
        });

        endpoints.MapPost("/api/events/payment", async (PaymentEvent payload, EventApplicationService service, CancellationToken ct) =>
        {
            try
            {
                var response = await service.PublishPaymentAsync(payload, ct);
                return Results.Created("/api/events/payment", response);
            }
            catch (EventValidationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message));
            }
        });

        return endpoints;
    }
}

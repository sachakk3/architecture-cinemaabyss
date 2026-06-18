using CinemaAbyss.Events.Domain;

namespace CinemaAbyss.Events.Application;

public static class EventValidators
{
    public static string? Validate(MovieEvent payload)
    {
        if (payload.MovieId <= 0) return "movie_id must be greater than 0";
        if (string.IsNullOrWhiteSpace(payload.Title)) return "title is required";
        if (string.IsNullOrWhiteSpace(payload.Action)) return "action is required";
        return null;
    }

    public static string? Validate(UserEvent payload)
    {
        if (payload.UserId <= 0) return "user_id must be greater than 0";
        if (string.IsNullOrWhiteSpace(payload.Action)) return "action is required";
        if (payload.Timestamp == default) return "timestamp is required";
        return null;
    }

    public static string? Validate(PaymentEvent payload)
    {
        if (payload.PaymentId <= 0) return "payment_id must be greater than 0";
        if (payload.UserId <= 0) return "user_id must be greater than 0";
        if (payload.Amount <= 0) return "amount must be greater than 0";
        if (string.IsNullOrWhiteSpace(payload.Status)) return "status is required";
        if (payload.Timestamp == default) return "timestamp is required";
        return null;
    }
}

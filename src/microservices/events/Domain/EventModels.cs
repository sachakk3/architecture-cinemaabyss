using System.Text.Json.Serialization;

namespace CinemaAbyss.Events.Domain;

public sealed record EventResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("partition")] int Partition,
    [property: JsonPropertyName("offset")] long Offset,
    [property: JsonPropertyName("event")] EventEnvelope Event);

public sealed record ErrorResponse([property: JsonPropertyName("error")] string Error);

public sealed record EventEnvelope(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("payload")] object Payload)
{
    public static EventEnvelope Create(string type, object payload) =>
        new($"{type}-{Guid.NewGuid():N}", type, DateTime.UtcNow, payload);
}

public sealed record MovieEvent(
    [property: JsonPropertyName("movie_id")] int MovieId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("user_id")] int? UserId,
    [property: JsonPropertyName("rating")] double? Rating,
    [property: JsonPropertyName("genres")] string[]? Genres,
    [property: JsonPropertyName("description")] string? Description);

public sealed record UserEvent(
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp);

public sealed record PaymentEvent(
    [property: JsonPropertyName("payment_id")] int PaymentId,
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("amount")] double Amount,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("method_type")] string? MethodType);

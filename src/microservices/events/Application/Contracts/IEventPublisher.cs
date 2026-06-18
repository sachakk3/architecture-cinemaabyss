using CinemaAbyss.Events.Domain;

namespace CinemaAbyss.Events.Application.Contracts;

public interface IEventPublisher
{
    Task<EventResponse> PublishAsync(string topic, EventEnvelope envelope, CancellationToken cancellationToken);
}

using CinemaAbyss.Events.Application.Contracts;
using CinemaAbyss.Events.Domain;

namespace CinemaAbyss.Events.Application;

public sealed class EventApplicationService
{
    private readonly EventsConfig _config;
    private readonly IEventPublisher _publisher;

    public EventApplicationService(EventsConfig config, IEventPublisher publisher)
    {
        _config = config;
        _publisher = publisher;
    }

    public Task<EventResponse> PublishMovieAsync(MovieEvent payload, CancellationToken cancellationToken)
    {
        var error = EventValidators.Validate(payload);
        if (error is not null)
        {
            throw new EventValidationException(error);
        }

        return PublishAsync(_config.MovieTopic, "movie", payload, cancellationToken);
    }

    public Task<EventResponse> PublishUserAsync(UserEvent payload, CancellationToken cancellationToken)
    {
        var error = EventValidators.Validate(payload);
        if (error is not null)
        {
            throw new EventValidationException(error);
        }

        return PublishAsync(_config.UserTopic, "user", payload, cancellationToken);
    }

    public Task<EventResponse> PublishPaymentAsync(PaymentEvent payload, CancellationToken cancellationToken)
    {
        var error = EventValidators.Validate(payload);
        if (error is not null)
        {
            throw new EventValidationException(error);
        }

        return PublishAsync(_config.PaymentTopic, "payment", payload, cancellationToken);
    }

    private Task<EventResponse> PublishAsync(string topic, string type, object payload, CancellationToken cancellationToken)
    {
        var envelope = EventEnvelope.Create(type, payload);
        return _publisher.PublishAsync(topic, envelope, cancellationToken);
    }
}

public sealed class EventValidationException : Exception
{
    public EventValidationException(string message) : base(message)
    {
    }
}

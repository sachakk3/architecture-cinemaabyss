using CinemaAbyss.Events.Application.Contracts;
using CinemaAbyss.Events.Domain;
using Confluent.Kafka;
using System.Text.Json;

namespace CinemaAbyss.Events.Infrastructure;

public sealed class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IProducer<string, string> producer, ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task<EventResponse> PublishAsync(string topic, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(envelope);
        var result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = envelope.Id,
            Value = payload
        }, cancellationToken);

        _logger.LogInformation(
            "Published event {EventId} of type {EventType} to topic {Topic} partition {Partition} offset {Offset}",
            envelope.Id,
            envelope.Type,
            topic,
            result.Partition.Value,
            result.Offset.Value);

        return new EventResponse("success", result.Partition.Value, result.Offset.Value, envelope);
    }
}

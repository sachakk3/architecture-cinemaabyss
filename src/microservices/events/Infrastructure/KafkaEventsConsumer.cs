using CinemaAbyss.Events.Domain;
using Confluent.Kafka;
using System.Text.Json;

namespace CinemaAbyss.Events.Infrastructure;

public sealed class KafkaEventsConsumer : BackgroundService
{
    private readonly EventsConfig _config;
    private readonly ILogger<KafkaEventsConsumer> _logger;

    public KafkaEventsConsumer(EventsConfig config, ILogger<KafkaEventsConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config.KafkaBrokers,
            GroupId = _config.KafkaGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(new[] { _config.MovieTopic, _config.UserTopic, _config.PaymentTopic });

        _logger.LogInformation(
            "Kafka consumer started. Topics: {MovieTopic}, {UserTopic}, {PaymentTopic}",
            _config.MovieTopic,
            _config.UserTopic,
            _config.PaymentTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var envelope = JsonSerializer.Deserialize<EventEnvelope>(consumeResult.Message.Value);

                    _logger.LogInformation(
                        "Consumed message from topic {Topic} partition {Partition} offset {Offset}: {Payload}",
                        consumeResult.Topic,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value,
                        consumeResult.Message.Value);

                    if (envelope is not null)
                    {
                        _logger.LogInformation(
                            "Processed event {EventId} of type {EventType} created at {Timestamp}",
                            envelope.Id,
                            envelope.Type,
                            envelope.Timestamp);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer stopping");
        }
        finally
        {
            consumer.Close();
        }
    }
}

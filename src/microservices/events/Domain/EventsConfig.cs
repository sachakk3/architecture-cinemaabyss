namespace CinemaAbyss.Events.Domain;

public sealed record EventsConfig(
    string KafkaBrokers,
    string KafkaGroupId,
    string MovieTopic,
    string UserTopic,
    string PaymentTopic);

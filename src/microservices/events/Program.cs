using CinemaAbyss.Events.Api;
using CinemaAbyss.Events.Application;
using CinemaAbyss.Events.Application.Contracts;
using CinemaAbyss.Events.Domain;
using CinemaAbyss.Events.Infrastructure;
using Confluent.Kafka;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

    var eventsConfig = BuildConfig(builder.Configuration);
    builder.Services.AddSingleton(eventsConfig);
    builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    {
        var config = new ProducerConfig
        {
            BootstrapServers = eventsConfig.KafkaBrokers,
            Acks = Acks.Leader,
            MessageTimeoutMs = 5000,
            SocketTimeoutMs = 5000
        };

        return new ProducerBuilder<string, string>(config).Build();
    });
    builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
    builder.Services.AddSingleton<EventApplicationService>();
    builder.Services.AddHostedService<KafkaEventsConsumer>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapEventEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Events service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

static EventsConfig BuildConfig(IConfiguration configuration)
{
    return new EventsConfig(
        KafkaBrokers: configuration["KAFKA_BROKERS"] ?? "localhost:9092",
        KafkaGroupId: configuration["KAFKA_GROUP_ID"] ?? "cinemaabyss-events-service",
        MovieTopic: configuration["KAFKA_TOPICS:Movie"] ?? "movie-events",
        UserTopic: configuration["KAFKA_TOPICS:User"] ?? "user-events",
        PaymentTopic: configuration["KAFKA_TOPICS:Payment"] ?? "payment-events");
}

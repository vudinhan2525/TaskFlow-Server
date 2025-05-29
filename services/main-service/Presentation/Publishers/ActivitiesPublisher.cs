using MainService.Domain.Entities;
using MainService.Domain.Interfaces;

using Confluent.Kafka;
using System.Text.Json;

public class ActivitiesPublisher : IPublisherService, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;

    private readonly ILogger<ActivitiesPublisher> _logger;


    public ActivitiesPublisher(IConfiguration configuration, ILogger<ActivitiesPublisher> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration.GetValue("KafkaHost", "localhost:9092"),
            Acks = Acks.All
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
        _topic = "activities";
        _logger = logger;
    }

    public async Task Emit<IActivitiesMessage>(IActivitiesMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var json = JsonSerializer.Serialize(message);
        await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
    }
    public async Task PublishActivity(ActivityDomain activity)
    {
        await Emit(activity);
    }

    public void Dispose() => _producer.Dispose();
}

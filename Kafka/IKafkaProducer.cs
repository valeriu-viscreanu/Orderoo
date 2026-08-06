using OrderApi.Events;

namespace OrderApi.Kafka
{
    public interface IKafkaProducer
    {
        Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    }
}

using System.Threading.Tasks;

namespace OrderApi.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent evt);
    }
}

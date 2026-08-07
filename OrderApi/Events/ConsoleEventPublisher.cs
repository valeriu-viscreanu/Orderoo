using System;
using System.Threading.Tasks;

namespace OrderApi.Events
{
    public class ConsoleEventPublisher : IEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent evt)
        {
            Console.WriteLine($"[Event Published] {evt}");
            return Task.CompletedTask;
        }
    }
}

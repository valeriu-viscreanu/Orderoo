using MediatR;

namespace OrderApi.Events
{
    public class ConsoleOrderCreatedEventHandler : INotificationHandler<OrderCreatedEvent>
    {
        public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Event Published via MediatR] {notification}");
            return Task.CompletedTask;
        }
    }
}

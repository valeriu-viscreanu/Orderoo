using MediatR;

namespace OrderApi.Events
{
    public record OrderCreatedEvent(int OrderId, string CustomerName, string Status, decimal TotalCost) : INotification;
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Commands;
using OrderApi.Data;
using OrderApi.Events;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IPublisher _publisher;

        public CreateOrderCommandHandler(AppDbContext context, IPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<Order> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            var order = new Order
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                Status = command.Status,
                TotalCost = command.TotalCost,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
            await _publisher.Publish(new OrderCreatedEvent(
                order.OrderId,
                $"{order.FirstName} {order.LastName}",
                order.TotalCost), cancellationToken);

            return order;
        }
    }
}

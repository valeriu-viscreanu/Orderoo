using MediatR;
using OrderApi.Commands;
using OrderApi.Data;
using OrderApi.Events;
using OrderApi.Kafka;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IPublisher _publisher;
        private readonly IKafkaProducer _kafkaProducer;

        public CreateOrderCommandHandler(AppDbContext context, IPublisher publisher, IKafkaProducer kafkaProducer)
        {
            _context = context;
            _publisher = publisher;
            _kafkaProducer = kafkaProducer;
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

            var orderCreatedEvent = new OrderCreatedEvent(
                order.OrderId,
                $"{order.FirstName} {order.LastName}",
                order.Status,
                order.TotalCost);

            // Publish to MediatR in-process handlers (e.g. console logger)
            await _publisher.Publish(orderCreatedEvent, cancellationToken);

            // Publish to Kafka topic "order"
            await _kafkaProducer.PublishAsync("order", orderCreatedEvent, cancellationToken);

            return order;
        }
    }
}

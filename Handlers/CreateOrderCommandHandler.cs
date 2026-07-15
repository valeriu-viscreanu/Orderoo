using FluentValidation;
using OrderApi.Commands;
using OrderApi.Data;
using OrderApi.Events;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CreateOrderCommand> _validator;
        private readonly IEventPublisher _eventPublisher;

        public CreateOrderCommandHandler(
            AppDbContext context, 
            IValidator<CreateOrderCommand> validator,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _validator = validator;
            _eventPublisher = eventPublisher;
        }

        public async Task<Order> Handle(
            CreateOrderCommand command,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

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

            await _eventPublisher.PublishAsync(new OrderCreatedEvent(
                order.OrderId, 
                $"{order.FirstName} {order.LastName}", 
                order.TotalCost));

            return order;
        }
    }
}

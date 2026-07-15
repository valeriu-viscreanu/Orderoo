using FluentValidation;
using OrderApi.Commands;
using OrderApi.Data;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CreateOrderCommand> _validator;

        public CreateOrderCommandHandler(AppDbContext context, IValidator<CreateOrderCommand> validator)
        {
            _context = context;
            _validator = validator;
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

            return order;
        }
    }
}

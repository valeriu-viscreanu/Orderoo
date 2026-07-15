using OrderApi.Commands;
using OrderApi.Data;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
    {
        private readonly AppDbContext _context;

        public CreateOrderCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> Handle(
            CreateOrderCommand command,
            CancellationToken cancellationToken = default)
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

            return order;
        }
    }
}

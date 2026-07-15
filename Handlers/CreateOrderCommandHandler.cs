using OrderApi.Commands;
using OrderApi.Data;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class CreateOrderCommandHandler
    {

        public static async Task<Order> Handle(
            CreateOrderCommand command,
            AppDbContext context)
        {
            var order = new Order
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                Status = command.Status,
                TotalCost = command.TotalCost,
                CreatedAt = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            return order;
        }
    }
}

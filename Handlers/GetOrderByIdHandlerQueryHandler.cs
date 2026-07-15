using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Queries;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class GetOrderByIdHandlerQueryHandler
    {
        public static async Task<Order?> Handle(GetOrderByIdQuery getOrderByIdQuery, AppDbContext appDbContext)
        {
            return await appDbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == getOrderByIdQuery.OrderId);
        }
    }
}

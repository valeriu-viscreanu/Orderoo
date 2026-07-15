using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Queries;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Order>
    {
        private readonly AppDbContext _appDbContext;

        public GetOrderByIdQueryHandler(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Order?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
        {
            return await _appDbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == query.OrderId, cancellationToken);
        }
    }
}

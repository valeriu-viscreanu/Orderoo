using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Queries;
using OrderManagement.Models;

namespace OrderApi.Handlers
{
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Order?>
    {
        private readonly AppDbContext _appDbContext;

        public GetOrderByIdQueryHandler(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
        {
            return await _appDbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == query.OrderId, cancellationToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Queries;
namespace OrderApi.Handlers
{
    public class GetOrderSummariesQueryHandler : IQueryHandler<GetOrderSummariesQuery, List<OrderSummaryDto>>
    {
        private readonly AppDbContext _context;

        public GetOrderSummariesQueryHandler(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task<List<OrderSummaryDto>?> HandleAsync(GetOrderSummariesQuery query, CancellationToken cancellationToken = default)
        {
            return await _context.Orders.Select(o => new OrderSummaryDto
            (
               o.OrderId,
               o.FirstName + o.LastName,
               o.Status,
               o.TotalCost
            )).ToListAsync();
        }
    }
}

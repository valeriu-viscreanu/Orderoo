using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Queries;

namespace OrderApi.Handlers
{
    public class GetOrderSummariesQueryHandler : IRequestHandler<GetOrderSummariesQuery, List<OrderSummaryDto>?>
    {
        private readonly AppDbContext _context;

        public GetOrderSummariesQueryHandler(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task<List<OrderSummaryDto>?> Handle(GetOrderSummariesQuery query, CancellationToken cancellationToken)
        {
            return await _context.Orders.Select(o => new OrderSummaryDto(
                o.OrderId,
                o.FirstName + " " + o.LastName,
                o.Status,
                o.TotalCost
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        }
    }
}

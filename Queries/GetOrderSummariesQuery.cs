using MediatR;
using System.Collections.Generic;

namespace OrderApi.Queries;

public record GetOrderSummariesQuery : IRequest<List<OrderSummaryDto>?>;
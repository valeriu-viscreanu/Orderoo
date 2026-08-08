using MediatR;
using OrderManagement.Models;

namespace OrderApi.Queries;

public record GetOrderByIdQuery : IRequest<Order?>
{
    public int OrderId { get; init; }
}
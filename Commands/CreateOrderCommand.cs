using MediatR;
using OrderManagement.Models;

namespace OrderApi.Commands;

public record CreateOrderCommand(string FirstName, string LastName, string Status, decimal TotalCost) : IRequest<Order>;

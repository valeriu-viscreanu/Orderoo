using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderApi.Commands;
using OrderApi.Data;
using OrderApi.Handlers;
using OrderApi.Queries;
using OrderManagement.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServerConnection")));

builder.Services.AddScoped<IQueryHandler<GetOrderByIdQuery, Order>, GetOrderByIdHandlerQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateOrderCommand, Order>, CreateOrderCommandHandler>();
builder.Services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();

var app = builder.Build();

app.UseHttpsRedirection();

// GET /api/orders
app.MapGet("/api/orders/{id}", async (int id, IQueryHandler<GetOrderByIdQuery, Order> handler, CancellationToken cancellationToken) =>
{
    var order = await handler.Handle(new GetOrderByIdQuery { OrderId = id }, cancellationToken);

    if (order is null)
    {
        return Results.NotFound("No orders found.");
    }

    return Results.Ok(order);
});

// POST /api/orders
app.MapPost("/api/orders", async (CreateOrderCommand command, ICommandHandler<CreateOrderCommand, Order> handler, CancellationToken cancellationToken) =>
{
    try
    {
        var order = await handler.Handle(command, cancellationToken);

        return Results.Created(
            $"/api/orders/{order.OrderId}",
            new { OrderId = order.OrderId });
    }
    catch (ValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors.ToDictionary(
            g => g.PropertyName,
            g => new[] { g.ErrorMessage }));
    }
});

app.Run();
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Behaviors;
using OrderApi.Commands;
using OrderApi.Data;
using OrderApi.Queries;
using OrderManagement.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServerConnection")));

// MediatR & Pipeline Behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

var app = builder.Build();

app.UseHttpsRedirection();

// GET /api/orders/{id}
app.MapGet("/api/orders/{id}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var order = await mediator.Send(new GetOrderByIdQuery { OrderId = id }, cancellationToken);

    if (order is null)
    {
        return Results.NotFound("No orders found.");
    }

    return Results.Ok(order);
});

// POST /api/orders
app.MapPost("/api/orders", async (CreateOrderCommand command, IMediator mediator, CancellationToken cancellationToken) =>
{
    try
    {
        var order = await mediator.Send(command, cancellationToken);

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
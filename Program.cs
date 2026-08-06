using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Behaviors;
using OrderApi.Commands;
using OrderApi.Data;
using OrderApi.Kafka;
using OrderApi.Queries;

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
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

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

Console.WriteLine("v 1.03");


// GET /api/orders/
app.MapGet("/api/orders/", async ( IMediator mediator, CancellationToken cancellationToken) =>
{
    var orders = await mediator.Send(new GetOrderSummariesQuery(), cancellationToken);

    if (orders is null)
    {
        return Results.NotFound("No orders found.");
    }

    return Results.Ok(orders);
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
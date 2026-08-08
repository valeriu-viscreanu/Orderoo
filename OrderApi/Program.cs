using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("admin");
            h.Password("admin12");
        });
    });
});

// Add rate limiting services
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "10m",
            Limit = 50
        }
    };
});

builder.Services.AddInMemoryRateLimiting();

var app = builder.Build();

// Configure rate limiting middleware
app.UseIpRateLimiting();

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

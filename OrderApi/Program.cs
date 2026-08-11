using AspNetCoreRateLimit;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Behaviors;
using OrderApi.Commands;
using OrderApi.Data;
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

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();

        cfg.Host(configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(configuration["RabbitMq:Username"]);
            h.Password(configuration["RabbitMq:Password"]);
        });
    });
});
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
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

// GET /api/orders
app.MapGet("/api/orders", async (IMediator mediator, CancellationToken cancellationToken) =>
{
    var orders = await mediator.Send(new GetOrderSummariesQuery(), cancellationToken);

    return Results.Ok(orders);
});


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Handlers;
using OrderApi.Queries;
using OrderManagement.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServerConnection")));

var app = builder.Build();

app.UseHttpsRedirection();

// GET /api/orders
app.MapGet("/api/orders/{id}", async (AppDbContext db, int id) =>
{
    var order = await GetOrderByIdHandlerQueryHandler.Handle(new GetOrderByIdQuery { OrderId = id }, db);

    if (order is null)
    {
        return Results.NotFound("No orders found.");
    }

    return Results.Ok(order);
});

// POST /api/orders
app.MapPost("/api/orders", async (Order order, AppDbContext db) =>
{
    order.OrderId = 0;
    order.CreatedAt = DateTime.UtcNow;

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/orders/{order.OrderId}",
        new { OrderId = order.OrderId });
});

app.Run();
using Microsoft.EntityFrameworkCore;
using OrderProcessor.Models;

namespace OrderProcessor.Data;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

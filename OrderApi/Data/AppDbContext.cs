using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;

namespace OrderApi.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<Order> Orders { get; set; }
    }
}

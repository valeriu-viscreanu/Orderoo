using Microsoft.EntityFrameworkCore;
using OrderProcessor.Data;
using OrderProcessor.Kafka;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));
builder.Services.AddHostedService<OrderProcessorWorker>();

var host = builder.Build();
host.Run();

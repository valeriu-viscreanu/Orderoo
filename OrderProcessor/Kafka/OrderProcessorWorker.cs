using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using OrderProcessor.Data;
using OrderProcessor.Messages;
using OrderProcessor.Models;
using System.Text.Json;

namespace OrderProcessor.Kafka;

public sealed class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private const string topic = "order";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _bootstrapServers;
    private readonly string _inputTopic;
    private readonly string _processedTopic;
    private readonly Random _random = new();


    public OrderProcessorWorker(ILogger<OrderProcessorWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");
        _inputTopic = configuration["Kafka:Topic"] ?? topic;
        _processedTopic = configuration["Kafka:ProcessedTopic"] ?? topic;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "order-processor",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(_inputTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(result.Message.Value);

                if (orderCreatedEvent is null)
                {
                    _logger.LogWarning("Skipping invalid message on topic {Topic}.", _inputTopic);
                    consumer.Commit(result);
                    continue;
                }

                var delaySeconds = _random.Next(3, 6);
                _logger.LogInformation("Processing order {OrderId} for {DelaySeconds}s.", orderCreatedEvent.OrderId, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderId == orderCreatedEvent.OrderId, stoppingToken);
                if (order is null)
                {
                    _logger.LogWarning("Order {OrderId} was not found.", orderCreatedEvent.OrderId);
                    consumer.Commit(result);
                    continue;
                }

                // Update database entity and event model
                order.Status = "Processed";
                orderCreatedEvent.Status = "Processed";

                await dbContext.SaveChangesAsync(stoppingToken);

                // Publish the updated event to the output topic
                var updatedPayload = JsonSerializer.Serialize(orderCreatedEvent);
                await producer.ProduceAsync(_processedTopic, new Message<string, string>
                {
                    Key = orderCreatedEvent.OrderId.ToString(),
                    Value = updatedPayload
                }, stoppingToken);

                _logger.LogInformation("Order {OrderId} status changed to Processed and published to {Topic}.", order.OrderId, _processedTopic);

                // Commit consumer offset after database update and event publish succeed
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume failure.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order processor failed.");
            }
        }
    }
}
namespace OrderApi.Events
{
    public record OrderCreatedEvent(int OrderId, string CustomerName, decimal TotalCost);
}

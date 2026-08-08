namespace OrderProcessor.Messages;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
}
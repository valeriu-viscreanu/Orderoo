namespace OrderManagement.Models;

public class Order
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public required string FirstName { get; set; } = string.Empty;
    public required string LastName { get; set; } = string.Empty;
    public required string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalCost { get; set; }
   // public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public Guid OrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

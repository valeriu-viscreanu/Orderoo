public record OrderSummaryDto(
    int OrderId,
    string CustomerName,
    string Status,
    decimal TotalCost
);
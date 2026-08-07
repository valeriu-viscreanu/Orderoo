public record OrderDto(
    int OrderId,
    string OrderNumber,
    string FirstName,
    string LastName,
    string Status,
    DateTime CreatedAt,
    decimal TotalCost
);
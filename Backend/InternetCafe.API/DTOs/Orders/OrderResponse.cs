namespace InternetCafe.API.DTOs.Orders;

public sealed record OrderLineResponse(
    int Product_ID,
    string Product_Name,
    int Quantity,
    decimal Unit_Price,
    decimal Line_Total);

public sealed record OrderResponse(
    int Order_ID,
    int Customer_ID,
    int? Computer_ID,
    string Status,
    decimal Total_Amount,
    decimal Balance_After,
    DateTime? Order_Date,
    IReadOnlyList<OrderLineResponse> Items);

public sealed record PendingOrderResponse(
    int Order_ID,
    int Customer_ID,
    string Customer_Name,
    int? Computer_ID,
    string? Computer_Name,
    string Status,
    decimal Total_Amount,
    DateTime? Order_Date,
    IReadOnlyList<OrderLineResponse> Items);

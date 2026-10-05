namespace InternetCafe.API.DTOs.Products;

public sealed record ProductResponse(
    int Product_ID,
    string Product_Name,
    decimal Price,
    int Stock_Quantity,
    string? Image_Url,
    int Category_ID,
    string Category_Name);

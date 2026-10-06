namespace InternetCafe.API.DTOs.Inventory;

public sealed record RestockResponse(
    int Inventory_Transaction_ID,
    int Product_ID,
    string Product_Name,
    int Quantity_Imported,
    int Stock_After,
    DateTime? Created_Date);

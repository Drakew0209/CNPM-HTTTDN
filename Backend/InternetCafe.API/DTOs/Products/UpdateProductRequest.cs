using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Products;

public sealed record UpdateProductRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Product_Name,
    [Range(typeof(decimal), "0.01", "9999999999.99")] decimal Price,
    [Range(0, int.MaxValue)] int Stock_Quantity,
    [Range(1, int.MaxValue)] int Category_ID);

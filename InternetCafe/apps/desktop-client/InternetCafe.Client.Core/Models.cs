using System.Globalization;
namespace InternetCafe.Client.Core;

public sealed record User(string Id, string Username, string FullName, string Role, string[] Permissions);
public sealed record LoginResult(string AccessToken, User User);
public sealed class Customer
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string Hobbies { get; set; } = "";
    public string Tier { get; set; } = "";
    public decimal Balance { get; set; }
    public string Status { get; set; } = "";
}
public sealed class Computer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Status { get; set; } = "";
    public bool Online { get; set; }
    public decimal HourlyRate { get; set; }
    public string? CustomerId { get; set; }
    public string? SessionId { get; set; }
    public string Display => $"{Name} · {Zone} · {Format.Money(HourlyRate)}/giờ";
}
public sealed class Session
{
    public string Id { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string ComputerId { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public decimal StartBalance { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string Started => Format.Time(StartTime);
    public string Ended => EndTime is { } time ? Format.Time(time) : "Đang sử dụng";
    public string Billed => Format.Money(Amount);
}
public sealed record Category(string Id, string Name);
public sealed class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool Active { get; set; }
    public string? ImageUrl { get; set; }
    public string PriceLabel => Format.Money(Price);
    public string StockLabel => Stock > 0 ? $"Còn {Stock}" : "Hết hàng";
}
public sealed class OrderLine
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
public sealed class Order
{
    public string Id { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string ComputerId { get; set; } = "";
    public List<OrderLine> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Details => string.Join(", ", Items.Select(x => $"{x.Name} ×{x.Quantity}"));
    public string TotalLabel => Format.Money(Total);
    public string Created => Format.Time(CreatedAt);
    public string StatusLabel => Format.Status(Status);
}
public sealed class Topup
{
    public string Id { get; set; } = "";
    public decimal Amount { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string AmountLabel => Format.Money(Amount);
    public string Created => Format.Time(CreatedAt);
    public string StatusLabel => Format.Status(Status);
}
public sealed class Transaction
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public string ReferenceId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string AmountLabel => Format.Money(Amount);
    public string Created => Format.Time(CreatedAt);
}
public sealed class Feedback
{
    public string Id { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string Status { get; set; } = "";
    public string Response { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string StatusLabel => Format.Status(Status);
    public string Created => Format.Time(CreatedAt);
}
public sealed record SurveyQuestion(string Id, string Text, string[] Options);
public sealed record SurveyResponse(string CustomerId, Dictionary<string, string> Answers, DateTimeOffset CreatedAt);
public sealed class Survey
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public List<SurveyQuestion> Questions { get; set; } = [];
    public List<string> CustomerIds { get; set; } = [];
    public List<SurveyResponse> Responses { get; set; } = [];
}
public sealed class Workspace
{
    public List<Computer> Computers { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
    public List<Topup> Topups { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
    public List<Feedback> Feedback { get; set; } = [];
    public List<Survey> Surveys { get; set; } = [];
}
public static class Format
{
    public static string Money(decimal amount) => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string Time(DateTimeOffset value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public static string Status(string status) => status switch
    { "Pending" => "Chờ xử lý", "Approved" => "Đã duyệt", "Rejected" => "Từ chối", "Preparing" => "Đang chuẩn bị", "Served" => "Đã phục vụ", "Cancelled" => "Đã hủy", "Processing" => "Đang xử lý", "Resolved" => "Đã giải quyết", "Active" => "Đang hoạt động", "Completed" => "Đã kết thúc", _ => status };
}

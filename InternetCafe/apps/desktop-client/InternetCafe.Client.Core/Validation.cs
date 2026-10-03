using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace InternetCafe.Client.Core;
public static partial class Validation
{
    public static string? Credentials(string username, string password)
        => string.IsNullOrWhiteSpace(username) ? "Nhập tên đăng nhập." : string.IsNullOrEmpty(password) ? "Nhập mật khẩu." : null;
    public static string? Profile(string name, string phone, string email, string dob)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) return "Họ tên bắt buộc, tối đa 100 ký tự.";
        if (!PhonePattern().IsMatch(phone)) return "Số điện thoại cần 9–12 chữ số, có thể bắt đầu bằng +.";
        if (!MailAddress.TryCreate(email, out var address) || address.Address != email) return "Địa chỉ email không hợp lệ.";
        if (!DateOnly.TryParseExact(dob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date > DateOnly.FromDateTime(DateTime.Today) || date.Year < 1900) return "Ngày sinh cần dạng yyyy-MM-dd, từ năm 1900 đến hôm nay.";
        return null;
    }
    public static string? Register(string username, string password, string confirmation, string name, string phone, string email, string dob)
    {
        if (!UsernamePattern().IsMatch(username)) return "Tên đăng nhập gồm 3–30 chữ cái, số hoặc dấu gạch dưới.";
        if (password.Length < 8) return "Mật khẩu cần ít nhất 8 ký tự.";
        if (password != confirmation) return "Hai mật khẩu chưa trùng nhau.";
        return Profile(name, phone, email, dob);
    }
    [GeneratedRegex(@"^\+?[0-9]{9,12}$")] private static partial Regex PhonePattern();
    [GeneratedRegex(@"^[a-zA-Z0-9_]{3,30}$")] private static partial Regex UsernamePattern();
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InternetCafe.Client.Core;

/// <summary>
/// Loại thông báo thống nhất cho cả Desktop và Web
/// </summary>
public enum NotificationType
{
    Success,
    Info,
    Warning,
    Error,
    Confirm
}

/// <summary>
/// Dữ liệu một thông báo
/// </summary>
public sealed record Notification(
    string Id,
    NotificationType Type,
    string Title,
    string Message,
    string? ActionText = null,
    TimeSpan? AutoCloseDelay = null,
    bool RequireInteraction = false
);

/// <summary>
/// Kết quả dialog xác nhận
/// </summary>
public sealed record ConfirmResult(bool Confirmed, string? InputText = null);

/// <summary>
/// Interface dịch vụ thông báo - implement riêng cho Desktop/Web
/// </summary>
public interface INotificationService
{
    /// <summary>Hiển thị toast thông báo (tự đóng sau delay)</summary>
    Task ShowToastAsync(Notification notification);

    /// <summary>Hiển thị dialog xác nhận (Yes/No hoặc OK/Cancel)</summary>
    Task<ConfirmResult> ShowConfirmAsync(string title, string message, string confirmText = "Xác nhận", string cancelText = "Hủy", NotificationType type = NotificationType.Confirm);

    /// <summary>Hiển thị dialog nhập liệu (prompt)</summary>
    Task<ConfirmResult> ShowPromptAsync(string title, string message, string placeholder = "", string confirmText = "OK", string cancelText = "Hủy");

    /// <summary>Đóng tất cả thông báo đang hiển thị</summary>
    void ClearAll();
}

/// <summary>
/// ViewModel cho một item toast trong danh sách
/// </summary>
public sealed partial class ToastItemViewModel : ObservableObject
{
    public string Id { get; }
    public NotificationType Type { get; }
    public string Title { get; }
    public string Message { get; }
    public string? ActionText { get; }
    public bool RequireInteraction { get; }

    [ObservableProperty] private bool isClosing = false;
    [ObservableProperty] private double progress = 1.0; // 1.0 -> 0.0

    public string Icon => Type switch
    {
        NotificationType.Success => "✓",
        NotificationType.Info => "ℹ",
        NotificationType.Warning => "⚠",
        NotificationType.Error => "✕",
        NotificationType.Confirm => "?",
        _ => "•"
    };

    public string TypeClass => Type.ToString().ToLowerInvariant();

    public ToastItemViewModel(Notification notification)
    {
        Id = notification.Id;
        Type = notification.Type;
        Title = notification.Title;
        Message = notification.Message;
        ActionText = notification.ActionText;
        RequireInteraction = notification.RequireInteraction;
    }

    public void StartAutoClose(TimeSpan delay, Action onComplete)
    {
        if (RequireInteraction) return;
        // Implementation moved to platform-specific service (DesktopNotificationService)
        // This is a placeholder for the Core library
    }
}

/// <summary>
/// Extension methods để dùng NotificationService dễ dàng trong ViewModel
/// </summary>
public static class NotificationExtensions
{
    public static async Task ShowSuccess(this INotificationService svc, string title, string message, TimeSpan? delay = null)
        => await svc.ShowToastAsync(new Notification(Guid.NewGuid().ToString(), NotificationType.Success, title, message, AutoCloseDelay: delay));

    public static async Task ShowInfo(this INotificationService svc, string title, string message, TimeSpan? delay = null)
        => await svc.ShowToastAsync(new Notification(Guid.NewGuid().ToString(), NotificationType.Info, title, message, AutoCloseDelay: delay));

    public static async Task ShowWarning(this INotificationService svc, string title, string message, TimeSpan? delay = null)
        => await svc.ShowToastAsync(new Notification(Guid.NewGuid().ToString(), NotificationType.Warning, title, message, AutoCloseDelay: delay));

    public static async Task ShowError(this INotificationService svc, string title, string message, TimeSpan? delay = null)
        => await svc.ShowToastAsync(new Notification(Guid.NewGuid().ToString(), NotificationType.Error, title, message, AutoCloseDelay: delay));

    public static async Task<bool> Confirm(this INotificationService svc, string title, string message, string confirmText = "Xác nhận", string cancelText = "Hủy")
    {
        var result = await svc.ShowConfirmAsync(title, message, confirmText, cancelText, NotificationType.Confirm);
        return result.Confirmed;
    }

    public static async Task<bool> ConfirmDanger(this INotificationService svc, string title, string message, string confirmText = "Xác nhận", string cancelText = "Hủy")
    {
        var result = await svc.ShowConfirmAsync(title, message, confirmText, cancelText, NotificationType.Error);
        return result.Confirmed;
    }

    public static async Task<string?> Prompt(this INotificationService svc, string title, string message, string placeholder = "", string confirmText = "OK", string cancelText = "Hủy")
    {
        var result = await svc.ShowPromptAsync(title, message, placeholder, confirmText, cancelText);
        return result.Confirmed ? result.InputText : null;
    }
}
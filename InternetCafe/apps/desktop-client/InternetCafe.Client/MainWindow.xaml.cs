using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using InternetCafe.Client.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

namespace InternetCafe.Client;

public partial class MainWindow : Window
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkEscape = 0x1B;
    private const int VkTab = 0x09;
    private const int VkF4 = 0x73;
    private const int VkF12 = 0x7B;
    private const int VkControl = 0x11;
    private const int VkAlt = 0x12;
    private const int VkShift = 0x10;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int KeyDownMask = unchecked((int)0x8000);
    private const string TaskManagerPolicyPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string TaskManagerPolicyName = "DisableTaskMgr";
    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);
    private readonly MainViewModel viewModel;
    private readonly IRealtimeUpdates realtime;
    private readonly DispatcherTimer syncTimer;
    private readonly DispatcherTimer clockTimer;
    private CompactWindow? compact;
    private readonly DispatcherTimer taskManagerGuard;
    private readonly LowLevelKeyboardProc keyboardProc;
    private readonly string emergencyPassword;
    private IntPtr keyboardHook;
    private object? originalTaskManagerPolicy;
    private RegistryValueKind? originalTaskManagerPolicyKind;
    private bool policyOriginalCaptured;
    private bool kioskProtectionEnabled;
    private bool emergencyBypassed;
    private bool emergencyPromptPending;
    private bool closing;
    private bool smokeMode;
    private string? lastPopupMessage;
    private DateTimeOffset lastPopupAt;
    public MainWindow(MainViewModel viewModel, IRealtimeUpdates realtime, IConfiguration config)
    {
        InitializeComponent();
        this.viewModel = viewModel; this.realtime = realtime; DataContext = viewModel;
        emergencyPassword = config["Kiosk:EmergencyPassword"] ?? "";
        keyboardProc = KeyboardHookCallback;
        viewModel.ConfirmAsync = message => Task.FromResult(MessageBox.Show(this, message, "Xác nhận thao tác", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes);
        viewModel.OnGameLaunched = () => Dispatcher.BeginInvoke(() => OpenCompact(this, new RoutedEventArgs()));
        syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Clamp(config.GetValue<int?>("Api:ResyncSeconds") ?? 5, 3, 60)) };
        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        taskManagerGuard = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        taskManagerGuard.Tick += OnTaskManagerGuardTick;
        syncTimer.Tick += OnPoll; clockTimer.Tick += OnTick;
        viewModel.PropertyChanged += OnViewModelChanged;
        realtime.Changed += OnRealtimeChanged; realtime.ConnectionChanged += OnRealtimeConnectionChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) => { UpdateKioskMode(); if (!viewModel.IsAuthenticated) { LoginPassword.Focus(); } };
    }
    private async void OnPoll(object? sender, EventArgs e) { if (!closing) await viewModel.PollAsync(); }
    private void OnTick(object? sender, EventArgs e) => viewModel.UpdateClock();
    private void OnRealtimeChanged() => Dispatcher.BeginInvoke(async () => { if (!closing) await viewModel.PollAsync(); });
    private void OnRealtimeConnectionChanged(string message) => Dispatcher.BeginInvoke(() => { if (!closing) viewModel.Notice = message; });
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsAuthenticated) && !viewModel.IsAuthenticated) emergencyBypassed = false;
        if (e.PropertyName == nameof(MainViewModel.IsAuthenticated))
        {
            if (viewModel.IsAuthenticated) { syncTimer.Start(); clockTimer.Start(); }
            else { syncTimer.Stop(); clockTimer.Stop(); compact?.Hide(); Show(); WindowState = WindowState.Normal; }
        }
        if (e.PropertyName == nameof(MainViewModel.IsAppLocked) && viewModel.IsAppLocked) { compact?.Hide(); Show(); WindowState = WindowState.Normal; Activate(); }
        if (e.PropertyName == nameof(MainViewModel.IsTemporaryLocked) && viewModel.IsTemporaryLocked) { compact?.Hide(); Show(); WindowState = WindowState.Normal; Activate(); }
        if (e.PropertyName is nameof(MainViewModel.IsAuthenticated) or nameof(MainViewModel.IsAppLocked) or nameof(MainViewModel.IsTemporaryLocked) or nameof(MainViewModel.HasActiveSession)) UpdateKioskMode();
        if (e.PropertyName is nameof(MainViewModel.Password) or nameof(MainViewModel.PasswordConfirmation) or nameof(MainViewModel.CurrentPassword) or nameof(MainViewModel.NewPassword) or nameof(MainViewModel.NewPasswordConfirmation)) ClearEmptyPasswords(this, e.PropertyName);
        if (e.PropertyName == nameof(MainViewModel.Notice)) ShowOperationPopup(viewModel.Notice, "Hoàn tất", MessageBoxImage.Information);
        if (e.PropertyName == nameof(MainViewModel.Error)) ShowOperationPopup(viewModel.Error, "Không thể thao tác", MessageBoxImage.Warning);
    }
    private void ShowOperationPopup(string message, string title, MessageBoxImage icon)
    {
        if (closing || smokeMode || string.IsNullOrWhiteSpace(message)) return;
        var now = DateTimeOffset.UtcNow;
        if (message == lastPopupMessage && now - lastPopupAt < TimeSpan.FromSeconds(10)) return;
        lastPopupMessage = message; lastPopupAt = now;
        Dispatcher.BeginInvoke(() =>
        {
        if (!closing) MessageBox.Show(this, message, title, MessageBoxButton.OK, icon);
        }, DispatcherPriority.Background);
    }
    private void UpdateKioskMode()
    {
        if (smokeMode || closing) return;
        if (emergencyBypassed)
        {
            WindowStyle = WindowStyle.SingleBorderWindow; Topmost = false; ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal; Width = 1280; Height = 850;
            SetKioskProtection(false);
            return;
        }
        var isLocked = !viewModel.IsAuthenticated || viewModel.IsAppLocked || viewModel.IsTemporaryLocked;
        if (isLocked)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            WindowState = WindowState.Maximized;
            compact?.Hide();
            Show();
            Activate();
            Focus();
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            Topmost = false;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
            Width = 1280;
            Height = 850;
        }
        SetKioskProtection(isLocked || viewModel.HasActiveSession);
    }
    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (smokeMode || !kioskProtectionEnabled) return;
        if (e.Key == System.Windows.Input.Key.System && e.SystemKey == System.Windows.Input.Key.F4) e.Handled = true;
    }
    private void SetKioskProtection(bool enabled)
    {
        enabled &= !emergencyBypassed && !smokeMode && !closing;
        if (enabled == kioskProtectionEnabled) return;
        kioskProtectionEnabled = enabled;
        if (enabled)
        {
            keyboardHook = SetWindowsHookEx(WhKeyboardLl, keyboardProc, GetModuleHandle(null), 0);
            taskManagerGuard.Start();
            SetTaskManagerPolicy(true);
        }
        else
        {
            taskManagerGuard.Stop();
            if (keyboardHook != IntPtr.Zero) { UnhookWindowsHookEx(keyboardHook); keyboardHook = IntPtr.Zero; }
            SetTaskManagerPolicy(false);
        }
    }
    private void SetTaskManagerPolicy(bool disabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(TaskManagerPolicyPath, writable: true);
            if (key is null) return;
            if (disabled)
            {
                if (!policyOriginalCaptured)
                {
                    policyOriginalCaptured = true;
                    if (key.GetValueNames().Contains(TaskManagerPolicyName, StringComparer.OrdinalIgnoreCase))
                    {
                        originalTaskManagerPolicy = key.GetValue(TaskManagerPolicyName);
                        originalTaskManagerPolicyKind = key.GetValueKind(TaskManagerPolicyName);
                    }
                }
                key.SetValue(TaskManagerPolicyName, 1, RegistryValueKind.DWord);
            }
            else if (policyOriginalCaptured)
            {
                if (originalTaskManagerPolicy is null) key.DeleteValue(TaskManagerPolicyName, throwOnMissingValue: false);
                else key.SetValue(TaskManagerPolicyName, originalTaskManagerPolicy, originalTaskManagerPolicyKind ?? RegistryValueKind.DWord);
                policyOriginalCaptured = false; originalTaskManagerPolicy = null; originalTaskManagerPolicyKind = null;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
    }
    private void OnTaskManagerGuardTick(object? sender, EventArgs e)
    {
        if (!kioskProtectionEnabled) return;
        var taskManagerFound = false;
        foreach (var process in Process.GetProcessesByName("taskmgr"))
        {
            try { process.Kill(); taskManagerFound = true; }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException) { }
            finally { process.Dispose(); }
        }
        if (taskManagerFound) { Topmost = true; Show(); Activate(); }
    }
    private IntPtr KeyboardHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown))
        {
            var key = Marshal.ReadInt32(lParam);
            var control = IsKeyDown(VkControl);
            var shift = IsKeyDown(VkShift);
            var alt = IsKeyDown(VkAlt);
            if (control && shift && key == VkF12)
            {
                if (!emergencyPromptPending) Dispatcher.BeginInvoke(new Action(ShowEmergencyBypassDialog));
                return (IntPtr)1;
            }
            if (key is VkLWin or VkRWin || (alt && key is VkTab or VkEscape or VkF4) || (control && key == VkEscape)) return (IntPtr)1;
        }
        return CallNextHookEx(keyboardHook, code, wParam, lParam);
    }
    private static bool IsKeyDown(int key) => (GetAsyncKeyState(key) & KeyDownMask) != 0;
    private void EmergencyBypass_Click(object sender, RoutedEventArgs e) => ShowEmergencyBypassDialog();
    private void ShowEmergencyBypassDialog()
    {
        if (emergencyPromptPending || smokeMode || closing) return;
        emergencyPromptPending = true;
        try
        {
            var passwordBox = new PasswordBox { MinWidth = 260, Margin = new Thickness(0, 12, 0, 12), MaxLength = 100 };
            var dialog = new Window { Title = "Thoát khẩn cấp", Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, SizeToContent = SizeToContent.WidthAndHeight, ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.ToolWindow, Topmost = true, Background = new SolidColorBrush(Color.FromRgb(12, 21, 43)), Foreground = Brushes.White };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Nhập mật khẩu quản trị để tạm dừng bảo vệ Kiosk.", TextWrapping = TextWrapping.Wrap, MaxWidth = 300 });
            stack.Children.Add(passwordBox);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new Button { Content = "Hủy", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
            var confirm = new Button { Content = "Mở khóa", MinWidth = 90, IsDefault = true };
            confirm.Click += (_, _) => dialog.DialogResult = true;
            buttons.Children.Add(cancel); buttons.Children.Add(confirm); stack.Children.Add(buttons);
            dialog.Content = new Border { Padding = new Thickness(22), Child = stack };
            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(emergencyPassword) && string.Equals(passwordBox.Password, emergencyPassword, StringComparison.Ordinal))
                {
                    emergencyBypassed = true;
                    SetKioskProtection(false);
                    UpdateKioskMode();
                    NoticeEmergencyBypass();
                }
                else MessageBox.Show(this, "Mật khẩu quản trị không chính xác.", "Không thể mở khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally { emergencyPromptPending = false; }
    }
    private void NoticeEmergencyBypass() => viewModel.Notice = "Đã tạm dừng bảo vệ Kiosk. Hãy đăng nhập lại để bật lại chế độ bảo vệ.";
    private void ClearEmptyPasswords(DependencyObject parent, string property)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is PasswordBox box && box.Tag as string == property && typeof(MainViewModel).GetProperty(property)?.GetValue(viewModel) is "") box.Clear();
            else ClearEmptyPasswords(child, property);
        }
    }
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel model || sender is not PasswordBox box) return;
        switch (box.Tag as string)
        {
            case "Password": model.Password = box.Password; break;
            case "PasswordConfirmation": model.PasswordConfirmation = box.Password; break;
            case "CurrentPassword": model.CurrentPassword = box.Password; break;
            case "NewPassword": model.NewPassword = box.Password; break;
            case "NewPasswordConfirmation": model.NewPasswordConfirmation = box.Password; break;
            case "TemporaryLockPassword": model.TemporaryLockPassword = box.Password; break;
        }
    }
    private void OpenCompact(object sender, RoutedEventArgs e)
    {
        if (!viewModel.IsAuthenticated || viewModel.IsAppLocked) return;
        compact ??= new CompactWindow(viewModel, this);
        compact.Show(); compact.Activate(); WindowState = WindowState.Minimized;
    }
    internal void RestoreMain() { Show(); WindowState = WindowState.Normal; Activate(); compact?.Hide(); }
    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        SetKioskProtection(false);
        taskManagerGuard.Tick -= OnTaskManagerGuardTick;
        closing = true;
        Application.Current.Shutdown();
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (closing || smokeMode) return;
        if (!emergencyBypassed && (!viewModel.IsAuthenticated || viewModel.IsAppLocked || viewModel.IsTemporaryLocked))
        {
            e.Cancel = true;
            return;
        }
        if (!smokeMode && viewModel.HasActiveSession && MessageBox.Show(this, "Phiên vẫn đang được máy chủ tính phí. Đóng ứng dụng không kết thúc phiên. Nên quay lại và chọn Kết thúc phiên. Vẫn đóng ứng dụng?", "Phiên đang hoạt động", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) { e.Cancel = true; return; }
        closing = true; SetKioskProtection(false); taskManagerGuard.Tick -= OnTaskManagerGuardTick; syncTimer.Stop(); clockTimer.Stop();
        syncTimer.Tick -= OnPoll; clockTimer.Tick -= OnTick; viewModel.PropertyChanged -= OnViewModelChanged;
        realtime.Changed -= OnRealtimeChanged; realtime.ConnectionChanged -= OnRealtimeConnectionChanged;
        if (compact is not null) { compact.AllowClose = true; compact.Close(); }
        await viewModel.ShutdownAsync();
    }
    public async Task RunSmokeAsync(string outputDirectory)
    {
        smokeMode = true; Directory.CreateDirectory(outputDirectory);
        using var trace = new TextWriterTraceListener(Path.Combine(outputDirectory, "binding-errors.log"));
        PresentationTraceSources.DataBindingSource.Listeners.Add(trace); PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        try
        {
            await CaptureAsync(this, Path.Combine(outputDirectory, "01-login.png"));
            viewModel.Username = "gamer"; viewModel.Password = "Demo@123"; await viewModel.LoginCommand.ExecuteAsync(null);
            if (!viewModel.IsAuthenticated || !viewModel.IsConnected) throw new InvalidOperationException("HTTP login/snapshot failed: " + viewModel.Error);
            for (var index = 0; index < 7; index++) { viewModel.SelectedTab = index; await CaptureAsync(this, Path.Combine(outputDirectory, $"{index + 2:00}-tab-{index}.png")); }
            compact = new CompactWindow(viewModel, this); compact.Show(); await CaptureAsync(compact, Path.Combine(outputDirectory, "09-compact.png")); compact.Hide();
            viewModel.IsAppLocked = true; await CaptureAsync(this, Path.Combine(outputDirectory, "10-lock.png")); viewModel.IsAppLocked = false;
            trace.Flush();
            File.WriteAllText(Path.Combine(outputDirectory, "smoke-result.txt"), $"PASS: live HTTP login; {viewModel.Products.Count} products; seven tabs, compact and lock rendered. Balance={viewModel.Balance}. No session or financial mutation performed.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(outputDirectory, "smoke-result.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(trace); Close(); }
    }
    private static async Task CaptureAsync(Window window, string path)
    {
        await Task.Delay(250); window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(path); encoder.Save(stream);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr moduleHandle, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}

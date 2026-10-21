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
    private readonly DispatcherTimer bannerTimer;
    private readonly DispatcherTimer idleTimer;
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
        viewModel.OnPlaySound = sound => AudioService.Play(sound);
        viewModel.OnSoundMuteChanged = muted => AudioService.IsMuted = muted;
        viewModel.OnCopyToClipboard = text =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch { }
        };
        viewModel.OnRemoteCommand = cmd => Dispatcher.BeginInvoke(() => ExecuteRemoteCommand(cmd));
        realtime.RemoteCommandReceived += (targetId, cmd) => viewModel.HandleRemoteCommand(targetId, cmd);
        viewModel.OnSpinWheelAnimation = async targetAngle =>
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.Invoke(() =>
            {
                var rotate = (RotateTransform)WheelContainer.RenderTransform;
                var anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = rotate.Angle,
                    To = targetAngle,
                    Duration = TimeSpan.FromMilliseconds(3200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                anim.Completed += (s, e) =>
                {
                    rotate.Angle = targetAngle;
                    tcs.TrySetResult(true);
                };

                var tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
                int ticks = 0;
                tickTimer.Tick += (s, e) =>
                {
                    ticks++;
                    if (ticks > 15 || !viewModel.IsSpinning) { tickTimer.Stop(); return; }
                    AudioService.Play("click");
                    if (ticks > 10) tickTimer.Interval = TimeSpan.FromMilliseconds(280);
                };
                tickTimer.Start();

                rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            });
            await tcs.Task;
        };
        
        AudioService.Initialize();
        syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Clamp(config.GetValue<int?>("Api:ResyncSeconds") ?? 5, 3, 60)) };
        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        bannerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5.5) };
        idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        taskManagerGuard = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        taskManagerGuard.Tick += OnTaskManagerGuardTick;
        syncTimer.Tick += OnPoll; clockTimer.Tick += OnTick;
        bannerTimer.Tick += OnBannerTimerTick;
        idleTimer.Tick += OnIdleTimerTick;
        viewModel.PropertyChanged += OnViewModelChanged;
        realtime.Changed += OnRealtimeChanged; realtime.ConnectionChanged += OnRealtimeConnectionChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        MainTabs.SelectionChanged += (s, e) => { if (e.Source == MainTabs && viewModel.IsAuthenticated && !closing && !smokeMode) AudioService.Play("click"); };
        Loaded += async (_, _) =>
        {
            syncTimer.Start();
            clockTimer.Start();
            bannerTimer.Start();
            viewModel.UpdateClock();
            UpdateKioskMode();
            await viewModel.CheckConnectivityAsync();
        };
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
            if (viewModel.IsAuthenticated) { bannerTimer.Stop(); idleTimer.Stop(); }
            else { bannerTimer.Start(); compact?.Hide(); Show(); WindowState = WindowState.Normal; }
        }
        if (e.PropertyName == nameof(MainViewModel.IsLoginModalOpen))
        {
            if (viewModel.IsLoginModalOpen)
            {
                idleTimer.Stop();
                idleTimer.Start();
                Dispatcher.BeginInvoke(() =>
                {
                    if (string.IsNullOrEmpty(viewModel.Username)) LoginUsername?.Focus();
                    else LoginPassword?.Focus();
                }, DispatcherPriority.Input);
            }
            else
            {
                idleTimer.Stop();
            }
        }
        if (e.PropertyName == nameof(MainViewModel.IsAppLocked) && viewModel.IsAppLocked) { compact?.Hide(); Show(); WindowState = WindowState.Normal; Activate(); }
        if (e.PropertyName == nameof(MainViewModel.IsTemporaryLocked) && viewModel.IsTemporaryLocked) { compact?.Hide(); Show(); WindowState = WindowState.Normal; Activate(); }
        if (e.PropertyName is nameof(MainViewModel.IsAuthenticated) or nameof(MainViewModel.IsAppLocked) or nameof(MainViewModel.IsTemporaryLocked) or nameof(MainViewModel.HasActiveSession)) UpdateKioskMode();
        if (e.PropertyName is nameof(MainViewModel.Password) or nameof(MainViewModel.PasswordConfirmation) or nameof(MainViewModel.CurrentPassword) or nameof(MainViewModel.NewPassword) or nameof(MainViewModel.NewPasswordConfirmation)) ClearEmptyPasswords(this, e.PropertyName);
        if (e.PropertyName == nameof(MainViewModel.Notice)) ShowOperationPopup(viewModel.Notice, "Hoàn tất", MessageBoxImage.Information);
        if (e.PropertyName == nameof(MainViewModel.Error)) ShowOperationPopup(viewModel.Error, "Không thể thao tác", MessageBoxImage.Warning);
    }
    private void ExecuteRemoteCommand(string cmd)
    {
        var isShutdown = string.Equals(cmd, "Shutdown", StringComparison.OrdinalIgnoreCase);
        var isRestart = string.Equals(cmd, "Restart", StringComparison.OrdinalIgnoreCase);
        if (!isShutdown && !isRestart) return;

        var actionName = isShutdown ? "TẮT MÁY (Shutdown)" : "KHỞI ĐỘNG LẠI (Restart)";
        viewModel.Notice = $"🚨 LỆNH TỪ MÁY CHỦ: Đang thực thi {actionName}...";
        AudioService.Play("error");

        if (smokeMode)
        {
            return;
        }

        try
        {
            var args = isShutdown ? "/s /t 0" : "/r /t 0";
            var psi = new ProcessStartInfo("shutdown", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            viewModel.Error = $"Không thể thực thi lệnh {actionName}: {ex.Message}";
        }
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
        if (smokeMode) return;
        if (kioskProtectionEnabled && e.Key == System.Windows.Input.Key.System && e.SystemKey == System.Windows.Input.Key.F4) e.Handled = true;

        if (!viewModel.IsAuthenticated)
        {
            if (!viewModel.IsLoginModalOpen)
            {
                if (e.Key != System.Windows.Input.Key.System && e.Key != System.Windows.Input.Key.LeftAlt && e.Key != System.Windows.Input.Key.RightAlt)
                {
                    viewModel.OpenLoginModalCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else
            {
                idleTimer.Stop();
                idleTimer.Start();
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    viewModel.CloseLoginModalCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
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
    private void OnBannerTimerTick(object? sender, EventArgs e)
    {
        if (!viewModel.IsAuthenticated && !viewModel.IsLoginModalOpen)
        {
            viewModel.NextBannerCommand.Execute(null);
        }
    }

    private void OnIdleTimerTick(object? sender, EventArgs e)
    {
        idleTimer.Stop();
        if (!viewModel.IsAuthenticated && viewModel.IsLoginModalOpen)
        {
            viewModel.CloseLoginModalCommand.Execute(null);
        }
    }

    private void OnScreensaverClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!viewModel.IsAuthenticated && !viewModel.IsLoginModalOpen)
        {
            viewModel.OpenLoginModalCommand.Execute(null);
        }
    }

    private void OnScreensaverBackgroundClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!viewModel.IsAuthenticated && viewModel.IsLoginModalOpen)
        {
            viewModel.CloseLoginModalCommand.Execute(null);
        }
    }

    private void OnLoginFieldKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            if (viewModel.IsRegistering) viewModel.RegisterCommand.Execute(null);
            else viewModel.LoginCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OpenCompact(object sender, RoutedEventArgs e)
    {
        if (!viewModel.IsAuthenticated || viewModel.IsAppLocked) return;
        compact ??= new CompactWindow(viewModel, this);
        compact.Show(); compact.Activate(); WindowState = WindowState.Minimized;
    }
    internal void RestoreMain() { if (closing) return; Show(); WindowState = WindowState.Normal; Activate(); compact?.Hide(); }
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
        closing = true; SetKioskProtection(false); taskManagerGuard.Tick -= OnTaskManagerGuardTick; syncTimer.Stop(); clockTimer.Stop(); bannerTimer.Stop(); idleTimer.Stop();
        syncTimer.Tick -= OnPoll; clockTimer.Tick -= OnTick; bannerTimer.Tick -= OnBannerTimerTick; idleTimer.Tick -= OnIdleTimerTick; viewModel.PropertyChanged -= OnViewModelChanged;
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
            await viewModel.CheckConnectivityAsync();
            viewModel.UpdateClock();
            await CaptureAsync(this, Path.Combine(outputDirectory, "00-screensaver.png"));
            viewModel.SelectBannerCommand.Execute("1");
            await CaptureAsync(this, Path.Combine(outputDirectory, "00-screensaver-1-lol.png"));
            viewModel.SelectBannerCommand.Execute("2");
            await CaptureAsync(this, Path.Combine(outputDirectory, "00-screensaver-2-valorant.png"));
            viewModel.SelectBannerCommand.Execute("0");
            viewModel.OpenLoginModalCommand.Execute(null);
            await CaptureAsync(this, Path.Combine(outputDirectory, "01-login.png"));
            viewModel.Username = "gamer"; viewModel.Password = "Demo@123"; await viewModel.LoginCommand.ExecuteAsync(null);
            if (!viewModel.IsAuthenticated || !viewModel.IsConnected) throw new InvalidOperationException("HTTP login/snapshot failed: " + viewModel.Error);
            for (var index = 0; index < 8; index++) { viewModel.SelectedTab = index; await CaptureAsync(this, Path.Combine(outputDirectory, $"{index + 2:00}-tab-{index}.png")); }
            compact = new CompactWindow(viewModel, this); compact.Show(); await CaptureAsync(compact, Path.Combine(outputDirectory, "09-compact.png")); compact.AllowClose = true; compact.Close(); compact = null;
            viewModel.IsAppLocked = true; await CaptureAsync(this, Path.Combine(outputDirectory, "10-lock.png")); viewModel.IsAppLocked = false;
            viewModel.OpenLuckyWheelCommand.Execute(null); await CaptureAsync(this, Path.Combine(outputDirectory, "11-lucky-wheel.png")); viewModel.CloseLuckyWheelCommand.Execute(null);
            trace.Flush();
            File.WriteAllText(Path.Combine(outputDirectory, "smoke-result.txt"), $"PASS: live HTTP login; {viewModel.Products.Count} products; seven tabs, compact and lock rendered. Balance={viewModel.Balance}. No session or financial mutation performed.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(outputDirectory, "smoke-result.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
        finally { closing = true; PresentationTraceSources.DataBindingSource.Listeners.Remove(trace); Application.Current.Shutdown(); }
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

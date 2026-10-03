using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using InternetCafe.Client.Core;
using Microsoft.Extensions.Configuration;

namespace InternetCafe.Client;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly IRealtimeUpdates realtime;
    private readonly DispatcherTimer syncTimer;
    private readonly DispatcherTimer clockTimer;
    private CompactWindow? compact;
    private bool closing;
    private bool smokeMode;
    public MainWindow(MainViewModel viewModel, IRealtimeUpdates realtime, IConfiguration config)
    {
        InitializeComponent();
        this.viewModel = viewModel; this.realtime = realtime; DataContext = viewModel;
        viewModel.ConfirmAsync = message => Task.FromResult(MessageBox.Show(this, message, "Xác nhận thao tác", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes);
        syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Clamp(config.GetValue<int?>("Api:ResyncSeconds") ?? 5, 3, 60)) };
        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        syncTimer.Tick += OnPoll; clockTimer.Tick += OnTick;
        viewModel.PropertyChanged += OnViewModelChanged;
        realtime.Changed += OnRealtimeChanged; realtime.ConnectionChanged += OnRealtimeConnectionChanged;
        Loaded += (_, _) => LoginPassword.Focus();
    }
    private async void OnPoll(object? sender, EventArgs e) { if (!closing) await viewModel.PollAsync(); }
    private void OnTick(object? sender, EventArgs e) => viewModel.UpdateClock();
    private void OnRealtimeChanged() => Dispatcher.BeginInvoke(async () => { if (!closing) await viewModel.PollAsync(); });
    private void OnRealtimeConnectionChanged(string message) => Dispatcher.BeginInvoke(() => { if (!closing) viewModel.Notice = message; });
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsAuthenticated))
        {
            if (viewModel.IsAuthenticated) { syncTimer.Start(); clockTimer.Start(); }
            else { syncTimer.Stop(); clockTimer.Stop(); compact?.Hide(); Show(); WindowState = WindowState.Normal; }
        }
        if (e.PropertyName == nameof(MainViewModel.IsAppLocked) && viewModel.IsAppLocked) { compact?.Hide(); Show(); WindowState = WindowState.Normal; Activate(); }
        if (e.PropertyName is nameof(MainViewModel.Password) or nameof(MainViewModel.PasswordConfirmation) or nameof(MainViewModel.CurrentPassword) or nameof(MainViewModel.NewPassword) or nameof(MainViewModel.NewPasswordConfirmation)) ClearEmptyPasswords(this, e.PropertyName);
    }
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
        }
    }
    private void OpenCompact(object sender, RoutedEventArgs e)
    {
        if (!viewModel.IsAuthenticated || viewModel.IsAppLocked) return;
        compact ??= new CompactWindow(viewModel, this);
        compact.Show(); compact.Activate(); WindowState = WindowState.Minimized;
    }
    internal void RestoreMain() { Show(); WindowState = WindowState.Normal; Activate(); compact?.Hide(); }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (closing) return;
        if (!smokeMode && viewModel.HasActiveSession && MessageBox.Show(this, "Phiên vẫn đang được máy chủ tính phí. Đóng ứng dụng không kết thúc phiên. Nên quay lại và chọn Kết thúc phiên. Vẫn đóng ứng dụng?", "Phiên đang hoạt động", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) { e.Cancel = true; return; }
        closing = true; syncTimer.Stop(); clockTimer.Stop();
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
}

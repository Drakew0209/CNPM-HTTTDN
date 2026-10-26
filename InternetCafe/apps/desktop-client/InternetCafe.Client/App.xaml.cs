using System.IO;
using System.Windows;
using InternetCafe.Client.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InternetCafe.Client;

public partial class App : Application
{
    private IHost? host;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory, Args = [] });
            builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false).AddEnvironmentVariables("INTERNETCAFE_");
            string? cliMachine = null;
            string? cliServer = null;
            var isWindowed = e.Args.Any(a => a is "--windowed" or "-w");
            for (var i = 0; i < e.Args.Length; i++)
            {
                if ((e.Args[i] is "--machine" or "-m" or "--pc") && i + 1 < e.Args.Length)
                {
                    cliMachine = e.Args[i + 1];
                }
                else if (e.Args[i].StartsWith("pc", StringComparison.OrdinalIgnoreCase) && !e.Args[i].Contains('/') && !e.Args[i].Contains('\\'))
                {
                    cliMachine = e.Args[i];
                }
                else if ((e.Args[i] is "--server" or "-s") && i + 1 < e.Args.Length)
                {
                    cliServer = e.Args[i + 1];
                }
            }
            var baseUrl = cliServer ?? builder.Configuration["Api:BaseUrl"] ?? "http://127.0.0.1:5055/api/v1/";
            if (!baseUrl.EndsWith("/api/v1/", StringComparison.OrdinalIgnoreCase))
            {
                var clean = baseUrl.TrimEnd('/');
                if (!clean.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase)) clean += "/api/v1";
                baseUrl = clean + "/";
            }
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiUri) || apiUri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Api:BaseUrl phải là địa chỉ HTTP hoặc HTTPS hợp lệ.");
            builder.Services.AddHttpClient<ICafeApi, SmartCafeApi>(http => { http.BaseAddress = apiUri; http.Timeout = TimeSpan.FromSeconds(15); });
            var realtimeUrl = !string.IsNullOrWhiteSpace(cliServer)
                ? cliServer.TrimEnd('/') + "/hubs/internetcafe"
                : builder.Configuration["Api:RealtimeUrl"] ?? "http://127.0.0.1:5055/hubs/internetcafe";
            builder.Services.AddSingleton<IRealtimeUpdates>(_ => builder.Configuration.GetValue<bool>("Api:RealtimeEnabled") ? new SignalRUpdates(realtimeUrl) : new PollingUpdates());
            builder.Services.AddSingleton<INotificationService, DesktopNotificationService>();
            var configuredMachine = cliMachine ?? builder.Configuration["Machine:Id"];
            var machineId = !string.IsNullOrWhiteSpace(configuredMachine)
                ? configuredMachine.Trim()
                : Environment.MachineName.ToLowerInvariant();
            isWindowed |= builder.Configuration.GetValue<bool>("Kiosk:Windowed");
            builder.Services.AddSingleton(services => new MainViewModel(services.GetRequiredService<ICafeApi>(), services.GetRequiredService<IRealtimeUpdates>(), services.GetRequiredService<INotificationService>(), machineId));
            builder.Services.AddSingleton<MainWindow>();
            host = builder.Build(); 
            await host.StartAsync();
            var window = host.Services.GetRequiredService<MainWindow>(); 
            MainWindow = window; 
            window.WindowedMode = isWindowed; 
            if (host.Services.GetService<INotificationService>() is DesktopNotificationService notificationService)
            {
                notificationService.SetOwner(window);
            }
            window.Show();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            if (e.Args.Length >= 2 && e.Args[0] == "--smoke") await window.RunSmokeAsync(e.Args[1]);
        }
        catch (Exception exception)
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup-error.txt"), exception.ToString()); } catch { }
            if (e.Args.Length >= 2 && e.Args[0] == "--smoke") { Directory.CreateDirectory(e.Args[1]); File.WriteAllText(Path.Combine(e.Args[1], "startup-error.txt"), exception.ToString()); }
            else MessageBox.Show(exception.Message, "Không thể mở InternetCafe", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null) { await host.StopAsync(TimeSpan.FromSeconds(3)); host.Dispose(); }
        base.OnExit(e);
    }
}

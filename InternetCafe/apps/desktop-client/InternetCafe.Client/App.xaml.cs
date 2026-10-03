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
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory, Args = [] });
            builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false).AddEnvironmentVariables("INTERNETCAFE_");
            var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://127.0.0.1:5055/api/v1/";
            if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var apiUri) || apiUri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Api:BaseUrl phải là địa chỉ HTTP hoặc HTTPS hợp lệ.");
            builder.Services.AddHttpClient<ICafeApi, CafeApi>(http => { http.BaseAddress = apiUri; http.Timeout = TimeSpan.FromSeconds(15); });
            builder.Services.AddSingleton<IRealtimeUpdates>(_ => builder.Configuration.GetValue<bool>("Api:RealtimeEnabled") ? new SignalRUpdates(builder.Configuration["Api:RealtimeUrl"] ?? "http://127.0.0.1:5055/hubs/cafe") : new PollingUpdates());
            builder.Services.AddSingleton(services => new MainViewModel(services.GetRequiredService<ICafeApi>(), services.GetRequiredService<IRealtimeUpdates>(), builder.Configuration["Machine:Id"] ?? "pc-01"));
            builder.Services.AddSingleton<MainWindow>();
            host = builder.Build(); await host.StartAsync();
            var window = host.Services.GetRequiredService<MainWindow>(); MainWindow = window; window.Show();
            if (e.Args.Length >= 2 && e.Args[0] == "--smoke") await window.RunSmokeAsync(e.Args[1]);
        }
        catch (Exception exception)
        {
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

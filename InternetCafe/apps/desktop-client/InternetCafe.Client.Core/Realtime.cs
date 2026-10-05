using Microsoft.AspNetCore.SignalR.Client;
namespace InternetCafe.Client.Core;

public interface IRealtimeUpdates : IAsyncDisposable
{
    event Action? Changed;
    event Action<string>? ConnectionChanged;
    Task StartAsync(Func<Task<string?>> token);
    Task StopAsync();
}
/// <summary>Production integration boundary. Demo host uses polling, never claims a SignalR connection.</summary>
public sealed class SignalRUpdates(string url) : IRealtimeUpdates
{
    private HubConnection? connection;
    public event Action? Changed;
    public event Action<string>? ConnectionChanged;
    public async Task StartAsync(Func<Task<string?>> token)
    {
        await StopAsync();
        connection = new HubConnectionBuilder().WithUrl(url, options => options.AccessTokenProvider = token).WithAutomaticReconnect().Build();
        foreach (var name in new[] { "ComputerChanged", "SessionStarted", "SessionEnded", "BalanceChanged", "TopupRequested", "TopupDecided", "OrderCreated", "OrderChanged" })
            connection.On<object>(name, _ => Changed?.Invoke());
        connection.Reconnecting += _ => { ConnectionChanged?.Invoke("SignalR mất kết nối — đang kết nối lại."); return Task.CompletedTask; };
        connection.Reconnected += _ => { ConnectionChanged?.Invoke("SignalR đã kết nối lại; đang tải snapshot."); Changed?.Invoke(); return Task.CompletedTask; };
        connection.Closed += _ => { ConnectionChanged?.Invoke("SignalR đã ngắt; HTTP vẫn đồng bộ định kỳ."); return Task.CompletedTask; };
        await connection.StartAsync();
        ConnectionChanged?.Invoke("SignalR đã kết nối.");
    }
    public async Task StopAsync() { if (connection is not null) { await connection.DisposeAsync(); connection = null; } }
    public async ValueTask DisposeAsync() => await StopAsync();
}
public sealed class PollingUpdates : IRealtimeUpdates
{
    public event Action? Changed { add { } remove { } }
    public event Action<string>? ConnectionChanged { add { } remove { } }
    public Task StartAsync(Func<Task<string?>> token) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

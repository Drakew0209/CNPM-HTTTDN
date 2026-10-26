using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
namespace InternetCafe.Client.Core;

public interface IRealtimeUpdates : IAsyncDisposable
{
    event Action? Changed;
    event Action<string>? ConnectionChanged;
    event Action<string, string>? RemoteCommandReceived;
    Task StartAsync(Func<Task<string?>> token);
    Task StopAsync();
}
/// <summary>Production integration boundary. Demo host uses polling, never claims a SignalR connection.</summary>
public sealed class SignalRUpdates(string url) : IRealtimeUpdates
{
    private HubConnection? connection;
    public string Url { get; set; } = url;
    public event Action? Changed;
    public event Action<string>? ConnectionChanged;
    public event Action<string, string>? RemoteCommandReceived;
    public async Task StartAsync(Func<Task<string?>> token)
    {
        await StopAsync();
        connection = new HubConnectionBuilder().WithUrl(Url, options => options.AccessTokenProvider = token).WithAutomaticReconnect().Build();
        foreach (var name in new[] { "ComputerChanged", "SessionStarted", "SessionEnded", "SessionChanged", "BalanceChanged", "TopupRequested", "TopupDecided", "OrderCreated", "OrderChanged" })
            connection.On<object>(name, _ => Changed?.Invoke());

        connection.On<JsonElement>("RemoteCommand", payload =>
        {
            try
            {
                var compId = payload.TryGetProperty("entityId", out var eid) ? eid.GetString() : null;
                var cmd = payload.TryGetProperty("data", out var d) && d.TryGetProperty("command", out var c) ? c.GetString() : null;
                if (!string.IsNullOrEmpty(compId) && !string.IsNullOrEmpty(cmd))
                {
                    RemoteCommandReceived?.Invoke(compId, cmd);
                }
            }
            catch { }
            Changed?.Invoke();
        });

        connection.Reconnecting += _ => { ConnectionChanged?.Invoke("SignalR mất kết nối — đang kết nối lại."); return Task.CompletedTask; };
        connection.Reconnected += _ => { ConnectionChanged?.Invoke("SignalR đã kết nối lại; đang tải snapshot."); Changed?.Invoke(); return Task.CompletedTask; };
        connection.Closed += _ => { ConnectionChanged?.Invoke("SignalR đã ngắt; HTTP vẫn đồng bộ định kỳ."); return Task.CompletedTask; };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await connection.StartAsync(timeout.Token);
        ConnectionChanged?.Invoke("SignalR đã kết nối.");
    }
    public async Task StopAsync() { if (connection is not null) { await connection.DisposeAsync(); connection = null; } }
    public async ValueTask DisposeAsync() => await StopAsync();
}
public sealed class PollingUpdates : IRealtimeUpdates
{
    public event Action? Changed { add { } remove { } }
    public event Action<string>? ConnectionChanged { add { } remove { } }
    public event Action<string, string>? RemoteCommandReceived { add { } remove { } }
    public Task StartAsync(Func<Task<string?>> token) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

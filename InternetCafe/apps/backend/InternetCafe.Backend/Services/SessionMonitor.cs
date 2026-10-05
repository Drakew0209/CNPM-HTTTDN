namespace InternetCafe.Backend.Services;

public sealed class SessionMonitor(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<SessionMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue<int?>("Backend:SessionMonitorSeconds") ?? 5, 2, 60));
        var offlineSessionGrace = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue<int?>("Backend:OfflineSessionGraceSeconds") ?? 300, 60, 3600));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<BusinessService>().CloseExpiredSessionsAsync(offlineSessionGrace, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Unable to evaluate expiring sessions."); }
        }
    }
}

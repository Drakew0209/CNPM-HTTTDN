using System.Net.Sockets;

namespace InternetCafe.Client.Core;

public sealed class SmartCafeApi(HttpClient client) : ICafeApi
{
    private readonly CafeApi realApi = new(client);
    private readonly StandaloneDemoApi demoApi = new();
    private bool? isStandalone;

    public bool IsStandalone => isStandalone == true;

    public string? AccessToken
    {
        get => isStandalone == true ? demoApi.AccessToken : realApi.AccessToken;
        set
        {
            demoApi.AccessToken = value;
            realApi.AccessToken = value;
        }
    }

    public DateTimeOffset? ServerTime => isStandalone == true ? demoApi.ServerTime : realApi.ServerTime;

    public void ResetMode() => isStandalone = null;

    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        if (isStandalone == true)
        {
            return await demoApi.GetAsync<T>(path, cancellationToken);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (isStandalone is null)
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(1500));
            }
            var result = await realApi.GetAsync<T>(path, cts.Token);
            isStandalone = false;
            return result;
        }
        catch (Exception ex)
        {
            if (ex is ApiException apiEx && apiEx.Code != "HTTP_ERROR")
            {
                isStandalone = false;
                throw;
            }
            isStandalone = true;
            return await demoApi.GetAsync<T>(path, cancellationToken);
        }
    }

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken = default)
    {
        if (isStandalone == true)
        {
            return await demoApi.SendAsync<T>(method, path, body, cancellationToken);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (isStandalone is null)
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(1500));
            }
            var result = await realApi.SendAsync<T>(method, path, body, cts.Token);
            isStandalone = false;
            return result;
        }
        catch (Exception ex)
        {
            if (ex is ApiException apiEx && apiEx.Code != "HTTP_ERROR")
            {
                isStandalone = false;
                throw;
            }
            isStandalone = true;
            return await demoApi.SendAsync<T>(method, path, body, cancellationToken);
        }
    }
}

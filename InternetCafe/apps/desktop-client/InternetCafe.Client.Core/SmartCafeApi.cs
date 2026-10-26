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

    public string? CustomBaseUrl
    {
        get => realApi.CustomBaseUrl;
        set => realApi.CustomBaseUrl = value;
    }

    public void ResetMode() => isStandalone = null;

    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = isStandalone == true ? 1500 : 6000;
            cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
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
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = isStandalone == true ? 2000 : 8000;
            cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
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

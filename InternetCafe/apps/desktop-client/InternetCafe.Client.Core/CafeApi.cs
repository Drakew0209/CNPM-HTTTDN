using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InternetCafe.Client.Core;

public interface ICafeApi
{
    string? AccessToken { get; set; }
    DateTimeOffset? ServerTime { get; }
    Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default);
    Task<T> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken = default);
}
public sealed class ApiException(string code, string message, HttpStatusCode status) : Exception(message)
{
    public string Code { get; } = code;
    public HttpStatusCode Status { get; } = status;
}
public sealed class CafeApi(HttpClient client) : ICafeApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public string? AccessToken { get; set; }
    public DateTimeOffset? ServerTime { get; private set; }
    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) => Request<T>(HttpMethod.Get, path, null, cancellationToken);
    public Task<T> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken = default) => Request<T>(method, path, body, cancellationToken);
    private async Task<T> Request<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (AccessToken is { Length: > 0 }) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await client.SendAsync(request, cancellationToken);
        ServerTime = response.Headers.Date;
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = payload.RootElement.TryGetProperty("error", out var value) ? value : default;
            var code = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var c) ? c.GetString() : "HTTP_ERROR";
            var message = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var m) ? m.GetString() : "Máy chủ không thể xử lý yêu cầu.";
            throw new ApiException(code ?? "HTTP_ERROR", message ?? "Yêu cầu thất bại.", response.StatusCode);
        }
        if (!payload.RootElement.TryGetProperty("data", out var data)) throw new JsonException("Response thiếu envelope data theo API contract.");
        return data.Deserialize<T>(Json) ?? throw new JsonException("Response data rỗng không đúng contract.");
    }
}

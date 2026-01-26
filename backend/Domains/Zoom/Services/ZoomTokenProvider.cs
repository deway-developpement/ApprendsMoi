using System.Net.Http.Headers;
using System.Text;

namespace backend.Domains.Zoom;

public class ZoomTokenProvider
{
    private readonly string? _accountId;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly HttpClient _httpClient;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ZoomTokenProvider(IConfiguration config, HttpClient httpClient)
    {
        _accountId = Environment.GetEnvironmentVariable("ZOOM_ACCOUNT_ID") ?? config["Zoom:AccountId"];
        _clientId = Environment.GetEnvironmentVariable("ZOOM_CLIENT_ID") ?? config["Zoom:ClientId"];
        _clientSecret = Environment.GetEnvironmentVariable("ZOOM_CLIENT_SECRET") ?? config["Zoom:ClientSecret"];
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.zoom.us/");
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedAccessToken!;
        }

        if (string.IsNullOrWhiteSpace(_accountId) || string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
        {
            throw new InvalidOperationException("Missing Zoom configuration (Account ID, Client ID, Client Secret)");
        }

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={_accountId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<backend.Database.Models.ZoomTokenResponse>(content);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Unable to obtain Zoom access token");
        }

        _cachedAccessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5 min buffer

        return _cachedAccessToken!;
    }
}

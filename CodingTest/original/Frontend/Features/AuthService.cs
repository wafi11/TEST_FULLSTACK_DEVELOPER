using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace Frontend.Features;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _js;

    private static Dictionary<string, int> _failedAttempts = new();
    private static Dictionary<string, DateTime> _lockedUntil = new();

    public AuthService(HttpClient httpClient, IJSRuntime js)
    {
        _httpClient = httpClient;
        _js = js;
    }

    // Helper — kirim request dengan token otomatis
    private async Task<HttpResponseMessage> SendWithToken(HttpRequestMessage request)
    {
        var token = await _js.InvokeAsync<string?>("localStorage.getItem", "token");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _httpClient.SendAsync(request);
    }

    public async Task<(bool Success, string Error)> Register(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/register", new { username, password });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Error ?? "Registration failed.");
        }

        return (true, "");
    }

  public async Task<(bool Success, string Error, bool IsLocked, int RemainingSeconds)> Login(string username, string password)
{
    var response = await _httpClient.PostAsJsonAsync("/api/login", new { username, password });

    if (!response.IsSuccessStatusCode)
    {
        var err = await response.Content.ReadFromJsonAsync<LoginErrorResponse>();

        // Kalau locked, simpan expiry ke localStorage
        if (err?.IsLocked == true && err.RemainingSeconds > 0)
        {
            var lockExpiry = DateTimeOffset.UtcNow.AddSeconds(err.RemainingSeconds).ToUnixTimeSeconds();
            await _js.InvokeVoidAsync("localStorage.setItem", "lockExpiry", lockExpiry.ToString());
        }

        return (false, err?.Error ?? "Login failed.", err?.IsLocked ?? false, err?.RemainingSeconds ?? 0);
    }

    // Login sukses — hapus lockExpiry
    await _js.InvokeVoidAsync("localStorage.removeItem", "lockExpiry");
    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
    await _js.InvokeVoidAsync("localStorage.setItem", "token", result!.Token);
    await _js.InvokeVoidAsync("localStorage.setItem", "loggedInUser", result.Username);

    return (true, "", false, 0);
}

public async Task<int> GetLockRemainingSeconds()
{
    var expiryStr = await _js.InvokeAsync<string?>("localStorage.getItem", "lockExpiry");
    if (string.IsNullOrEmpty(expiryStr)) return 0;

    var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiryStr));
    var remaining = (int)(expiry - DateTimeOffset.UtcNow).TotalSeconds;
    return remaining > 0 ? remaining : 0;
}
public record LoginResponse(string Token, string Username);
public record LoginErrorResponse(string Error, bool IsLocked, int RemainingSeconds);

    public async Task Logout()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/logout");
        await SendWithToken(request);

        await _js.InvokeVoidAsync("localStorage.removeItem", "token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "loggedInUser");
    }

    public async Task<UserDto?> GetCurrentUser()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        var response = await SendWithToken(request); // ← token otomatis injected
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<string?> GetLoggedInUser()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", "loggedInUser");
    }

    public bool IsLocked(string username)
    {
        if (_lockedUntil.TryGetValue(username, out var lockedUntil))
            return DateTime.UtcNow < lockedUntil;
        return false;
    }
}

public record LoginResponse(string Token, string Username);
public record ErrorResponse(string Error);
public record UserDto(int Id, string Username, string Email, DateTime CreatedAt);
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TopTL;

/// <summary>
/// C# client for the TOP.TL API.
///
/// <code>
/// using var client = new TopTLClient("your-api-token");
/// var listing = await client.GetListingAsync("mybot");
/// </code>
/// </summary>
public class TopTLClient : IDisposable
{
    public const string DefaultBaseUrl = "https://top.tl/api/v1";

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Creates a new TOP.TL client with the default base URL.
    /// </summary>
    /// <param name="token">API bearer token</param>
    /// <param name="baseUrl">Optional custom base URL</param>
    public TopTLClient(string token, string baseUrl = DefaultBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("API token must not be null or empty", nameof(token));

        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("toptl-csharp/1.0.0");
        _http.Timeout = TimeSpan.FromSeconds(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Gets listing information for the given username.
    /// </summary>
    public async Task<Listing> GetListingAsync(string username, CancellationToken ct = default)
    {
        return await GetAsync<Listing>($"/listing/{username}", ct);
    }

    /// <summary>
    /// Gets votes for the given listing.
    /// </summary>
    public async Task<VotesResponse> GetVotesAsync(string username, CancellationToken ct = default)
    {
        return await GetAsync<VotesResponse>($"/listing/{username}/votes", ct);
    }

    /// <summary>
    /// Checks whether a user has voted for a listing.
    /// </summary>
    public async Task<VoteCheck> HasVotedAsync(string username, string userId, CancellationToken ct = default)
    {
        return await GetAsync<VoteCheck>($"/listing/{username}/has-voted/{userId}", ct);
    }

    /// <summary>
    /// Posts stats (member count, group count) for a listing.
    /// </summary>
    public async Task PostStatsAsync(string username, StatsUpdate stats, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/listing/{username}/stats", stats, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets global TOP.TL statistics.
    /// </summary>
    public async Task<Stats> GetStatsAsync(CancellationToken ct = default)
    {
        return await GetAsync<Stats>("/stats", ct);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var response = await _http.GetAsync($"{_baseUrl}{path}", ct);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(content, _jsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response from {path}");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

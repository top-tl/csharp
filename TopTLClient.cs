using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TopTL;

/// <summary>
/// Official C# client for the TOP.TL public API.
/// </summary>
/// <example>
/// <code>
/// using var client = new TopTLClient("toptl_xxx");
/// var listing = await client.GetListingAsync("mybot");
/// Console.WriteLine(listing.Title + " — " + listing.VoteCount + " votes");
/// </code>
/// </example>
public class TopTLClient : IDisposable
{
    /// <summary>Default API root. Per-method paths add <c>/v1/</c>.</summary>
    public const string DefaultBaseUrl = "https://top.tl/api";

    /// <summary>SDK version, sent as part of the User-Agent header.</summary>
    public const string SdkVersion = "0.1.0";

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Creates a client that manages its own <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="apiKey">Bearer API key from https://top.tl/profile → API Keys.</param>
    /// <param name="baseUrl">Override only for self-hosted / staging environments.</param>
    /// <param name="timeout">Seconds before any HTTP request is abandoned. Default 15s.</param>
    /// <param name="userAgent">Optional UA suffix (e.g. <c>"mybot/1.0"</c>) appended to the SDK's UA.</param>
    public TopTLClient(
        string apiKey,
        string baseUrl = DefaultBaseUrl,
        TimeSpan? timeout = null,
        string? userAgent = null)
        : this(apiKey, CreateDefaultClient(), ownsHttpClient: true, baseUrl, timeout, userAgent)
    {
    }

    /// <summary>
    /// Creates a client that shares an <see cref="HttpClient"/> supplied by the caller
    /// (e.g. from <c>IHttpClientFactory</c>). The supplied client will NOT be disposed.
    /// </summary>
    /// <param name="apiKey">Bearer API key.</param>
    /// <param name="httpClient">Caller-managed <see cref="HttpClient"/>.</param>
    /// <param name="baseUrl">Override only for self-hosted / staging environments.</param>
    /// <param name="timeout">Seconds before any HTTP request is abandoned. Default 15s.</param>
    /// <param name="userAgent">Optional UA suffix appended to the SDK's UA.</param>
    public TopTLClient(
        string apiKey,
        HttpClient httpClient,
        string baseUrl = DefaultBaseUrl,
        TimeSpan? timeout = null,
        string? userAgent = null)
        : this(apiKey, httpClient, ownsHttpClient: false, baseUrl, timeout, userAgent)
    {
    }

    private TopTLClient(
        string apiKey,
        HttpClient httpClient,
        bool ownsHttpClient,
        string baseUrl,
        TimeSpan? timeout,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("apiKey is required", nameof(apiKey));
        if (httpClient is null)
            throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("baseUrl is required", nameof(baseUrl));

        _http = httpClient;
        _ownsHttpClient = ownsHttpClient;
        _baseUrl = baseUrl.TrimEnd('/');

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (!_http.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var ua = $"toptl-csharp/{SdkVersion}";
        if (!string.IsNullOrWhiteSpace(userAgent))
            ua = $"{ua} {userAgent}";
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(ua);

        if (timeout.HasValue)
            _http.Timeout = timeout.Value;
        else if (_ownsHttpClient)
            _http.Timeout = TimeSpan.FromSeconds(15);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    private static HttpClient CreateDefaultClient() => new HttpClient();

    // ------------------------------------------------------------------
    // Public endpoints
    // ------------------------------------------------------------------

    /// <summary>
    /// Fetch a listing by its Telegram username (without the leading <c>@</c>).
    /// </summary>
    public Task<Listing> GetListingAsync(string username, CancellationToken ct = default)
    {
        RequireUsername(username);
        return SendAsync<Listing>(HttpMethod.Get, $"/v1/listing/{Uri.EscapeDataString(username)}", null, ct)!;
    }

    /// <summary>
    /// Recent voters for a listing (most recent first).
    /// </summary>
    /// <param name="username">Listing username without the leading <c>@</c>.</param>
    /// <param name="limit">Max voters to return (server default 20, max typically 100).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<Voter>> GetVotesAsync(string username, int limit = 20, CancellationToken ct = default)
    {
        RequireUsername(username);
        var path = $"/v1/listing/{Uri.EscapeDataString(username)}/votes?limit={limit}";
        using var response = await SendRawAsync(HttpMethod.Get, path, null, ct).ConfigureAwait(false);
        var body = await ReadAndValidateAsync(response, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body)) return new List<Voter>();

        using var doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<Voter>>(root.GetRawText(), _jsonOptions) ?? new List<Voter>();
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<Voter>>(items.GetRawText(), _jsonOptions) ?? new List<Voter>();
        return new List<Voter>();
    }

    /// <summary>
    /// Has this Telegram user id voted for the given listing?
    /// </summary>
    public Task<VoteCheck> HasVotedAsync(string username, string userId, CancellationToken ct = default)
    {
        RequireUsername(username);
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId is required", nameof(userId));
        return SendAsync<VoteCheck>(
            HttpMethod.Get,
            $"/v1/listing/{Uri.EscapeDataString(username)}/has-voted/{Uri.EscapeDataString(userId)}",
            null,
            ct)!;
    }

    /// <inheritdoc cref="HasVotedAsync(string,string,CancellationToken)"/>
    public Task<VoteCheck> HasVotedAsync(string username, long userId, CancellationToken ct = default)
        => HasVotedAsync(username, userId.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);

    /// <summary>
    /// Update counters on a listing you own. Only non-null fields are sent;
    /// the others are left untouched on the server.
    /// </summary>
    public async Task<StatsResult> PostStatsAsync(string username, StatsPayload stats, CancellationToken ct = default)
    {
        RequireUsername(username);
        if (stats is null) throw new ArgumentNullException(nameof(stats));
        if (stats.MemberCount is null && stats.GroupCount is null && stats.ChannelCount is null && stats.BotServes is null)
        {
            throw new ArgumentException(
                "PostStatsAsync needs at least one of MemberCount, GroupCount, ChannelCount, or BotServes.",
                nameof(stats));
        }
        var result = await SendAsync<StatsResult>(
            HttpMethod.Post,
            $"/v1/listing/{Uri.EscapeDataString(username)}/stats",
            stats,
            ct).ConfigureAwait(false);
        return result ?? new StatsResult { Success = true, Username = username };
    }

    /// <summary>
    /// Post stats for up to 25 listings in one request.
    /// </summary>
    public async Task<List<StatsResult>> BatchPostStatsAsync(
        IEnumerable<BatchStatsItem> items,
        CancellationToken ct = default)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        var list = new List<BatchStatsItem>(items);
        if (list.Count == 0) return new List<StatsResult>();
        var result = await SendAsync<List<StatsResult>>(
            HttpMethod.Post,
            "/v1/stats/batch",
            list,
            ct).ConfigureAwait(false);
        return result ?? new List<StatsResult>();
    }

    /// <summary>
    /// Register the URL TOP.TL should POST to whenever someone votes for the listing.
    /// </summary>
    public async Task<WebhookConfig> SetWebhookAsync(
        string username,
        string url,
        string? rewardTitle = null,
        CancellationToken ct = default)
    {
        RequireUsername(username);
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("url is required", nameof(url));
        var body = new WebhookConfig { Url = url, RewardTitle = rewardTitle };
        var result = await SendAsync<WebhookConfig>(
            HttpMethod.Put,
            $"/v1/listing/{Uri.EscapeDataString(username)}/webhook",
            body,
            ct).ConfigureAwait(false);
        return result ?? new WebhookConfig { Url = url, RewardTitle = rewardTitle };
    }

    /// <summary>
    /// Send a synthetic vote event to the listing's configured webhook.
    /// </summary>
    public async Task<WebhookTestResult> TestWebhookAsync(string username, CancellationToken ct = default)
    {
        RequireUsername(username);
        var result = await SendAsync<WebhookTestResult>(
            HttpMethod.Post,
            $"/v1/listing/{Uri.EscapeDataString(username)}/webhook/test",
            null,
            ct).ConfigureAwait(false);
        return result ?? new WebhookTestResult { Success = false };
    }

    /// <summary>
    /// Site-wide totals (channels / groups / bots / total listings).
    /// </summary>
    public Task<GlobalStats> GetGlobalStatsAsync(CancellationToken ct = default)
        => SendAsync<GlobalStats>(HttpMethod.Get, "/v1/stats", null, ct)!;

    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------

    private static void RequireUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("username is required", nameof(username));
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, path, body, ct).ConfigureAwait(false);
        var text = await ReadAndValidateAsync(response, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(text, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new TopTLException(
                $"Failed to deserialize response from {path}: {ex.Message}",
                statusCode: (int)response.StatusCode,
                responseBody: text,
                inner: ex);
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        var url = _baseUrl + path;
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, body.GetType(), _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        try
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new TopTLException($"HTTP transport error calling {url}: {ex.Message}", inner: ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TopTLException($"Request to {url} timed out.", inner: ex);
        }
    }

    private static async Task<string> ReadAndValidateAsync(HttpResponseMessage response, CancellationToken ct)
    {
#if NET6_0_OR_GREATER
        var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
        var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        if (response.IsSuccessStatusCode) return text;

        var status = (int)response.StatusCode;
        var message = ExtractErrorMessage(text) ?? $"HTTP {status} {response.ReasonPhrase}";

        throw status switch
        {
            401 or 403 => new TopTLAuthenticationException(message, status, text),
            404 => new TopTLNotFoundException(message, status, text),
            429 => new TopTLRateLimitException(message, status, text),
            >= 400 and < 500 => new TopTLValidationException(message, status, text),
            _ => new TopTLException(message, status, text),
        };
    }

    private static string? ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var key in new[] { "error", "message", "detail" })
            {
                if (doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
        }
        catch (JsonException)
        {
            // not JSON — fall through
        }
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsHttpClient) _http.Dispose();
        GC.SuppressFinalize(this);
    }
}

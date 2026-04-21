using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TopTL;

/// <summary>
/// Periodically posts stats to TOP.TL in the background. Designed for long-running
/// bot processes. For one-shot cron jobs, call <see cref="TopTLClient.PostStatsAsync"/>
/// directly instead.
/// </summary>
/// <example>
/// <code>
/// using var client = new TopTLClient("toptl_xxx");
/// var poster = new Autoposter(
///     client,
///     "mybot",
///     async ct => new StatsPayload { MemberCount = await bot.GetMemberCountAsync(ct) },
///     interval: TimeSpan.FromMinutes(30),
///     onlyOnChange: true);
/// poster.OnError += (_, err) => Console.WriteLine(err.Message);
/// poster.Start();
/// // ...on shutdown:
/// await poster.StopAsync();
/// </code>
/// </example>
public class Autoposter : IDisposable, IAsyncDisposable
{
    private readonly TopTLClient _client;
    private readonly string _username;
    private readonly Func<CancellationToken, Task<StatsPayload?>> _statsProvider;
    private readonly TimeSpan _interval;
    private readonly bool _onlyOnChange;
    private readonly object _sync = new();

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private StatsPayload? _last;
    private bool _disposed;

    /// <summary>Raised when a post (or the callback) throws.</summary>
    public event EventHandler<Exception>? OnError;

    /// <summary>Raised after a successful stats post.</summary>
    public event EventHandler<StatsPayload>? OnPost;

    /// <summary>
    /// Creates an autoposter with an async stats provider.
    /// </summary>
    /// <param name="client">TOP.TL client.</param>
    /// <param name="username">Listing username (no leading <c>@</c>).</param>
    /// <param name="statsProvider">Callback returning the current stats. Return <c>null</c> to skip a tick.</param>
    /// <param name="interval">Time between posts. Default 30 minutes.</param>
    /// <param name="onlyOnChange">When true, skip posting if counters equal the last-posted values.</param>
    public Autoposter(
        TopTLClient client,
        string username,
        Func<CancellationToken, Task<StatsPayload?>> statsProvider,
        TimeSpan? interval = null,
        bool onlyOnChange = false)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _username = !string.IsNullOrWhiteSpace(username)
            ? username
            : throw new ArgumentException("username is required", nameof(username));
        _statsProvider = statsProvider ?? throw new ArgumentNullException(nameof(statsProvider));
        _interval = interval ?? TimeSpan.FromMinutes(30);
        _onlyOnChange = onlyOnChange;
    }

    /// <summary>
    /// Convenience overload for synchronous stats callbacks.
    /// </summary>
    public Autoposter(
        TopTLClient client,
        string username,
        Func<StatsPayload?> statsProvider,
        TimeSpan? interval = null,
        bool onlyOnChange = false)
        : this(
            client,
            username,
            _ => Task.FromResult(statsProvider is null ? null : statsProvider()),
            interval,
            onlyOnChange)
    {
    }

    /// <summary>
    /// Starts the background loop. Subsequent calls while running are ignored.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Autoposter));
            if (_loop is { IsCompleted: false }) return;
            _cts = new CancellationTokenSource();
            _loop = RunAsync(_cts.Token);
        }
    }

    /// <summary>
    /// Runs one tick immediately, synchronously (well — awaitably).
    /// Useful from within a cron job.
    /// </summary>
    public Task PostOnceAsync(CancellationToken ct = default) => TickAsync(ct);

    /// <summary>
    /// Signals the loop to stop and awaits the in-flight tick (if any).
    /// </summary>
    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? loop;
        lock (_sync)
        {
            cts = _cts;
            loop = _loop;
            _cts = null;
            _loop = null;
        }
        if (cts is null) return;
        try
        {
            cts.Cancel();
            if (loop is not null)
            {
                try { await loop.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await TickAsync(ct).ConfigureAwait(false);
            try
            {
                await Task.Delay(_interval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        StatsPayload? stats;
        try
        {
            stats = await _statsProvider(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            return;
        }

        if (stats is null) return;
        if (_onlyOnChange && StatsEqual(stats, _last)) return;

        try
        {
            await _client.PostStatsAsync(_username, stats, ct).ConfigureAwait(false);
            _last = stats;
            OnPost?.Invoke(this, stats);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private static bool StatsEqual(StatsPayload a, StatsPayload? b)
    {
        if (b is null) return false;
        if (a.MemberCount != b.MemberCount) return false;
        if (a.GroupCount != b.GroupCount) return false;
        if (a.ChannelCount != b.ChannelCount) return false;
        return SequenceEqualOrBothEmpty(a.BotServes, b.BotServes);
    }

    private static bool SequenceEqualOrBothEmpty(List<string>? a, List<string>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return (a?.Count ?? 0) == (b?.Count ?? 0);
        return a.SequenceEqual(b);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { StopAsync().GetAwaiter().GetResult(); }
        catch { /* swallow on dispose */ }
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await StopAsync().ConfigureAwait(false); }
        catch { /* swallow on dispose */ }
        GC.SuppressFinalize(this);
    }
}

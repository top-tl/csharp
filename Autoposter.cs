namespace TopTL;

/// <summary>
/// Automatically posts stats to TOP.TL at a fixed interval.
///
/// <code>
/// using var client = new TopTLClient("your-api-token");
/// var autoposter = new Autoposter(client, "mybot", async () =>
///     new StatsUpdate(MemberCount: await bot.GetMemberCountAsync()));
/// autoposter.OnError += (_, e) => Console.WriteLine($"Error: {e.Message}");
/// autoposter.Start(TimeSpan.FromMinutes(30));
///
/// // When shutting down:
/// autoposter.Stop();
/// </code>
/// </summary>
public class Autoposter : IDisposable
{
    private readonly TopTLClient _client;
    private readonly string _username;
    private readonly Func<Task<StatsUpdate>> _statsProvider;
    private Timer? _timer;
    private bool _disposed;

    /// <summary>
    /// Raised when a stats post fails.
    /// </summary>
    public event EventHandler<Exception>? OnError;

    /// <summary>
    /// Raised after a successful stats post.
    /// </summary>
    public event EventHandler<StatsUpdate>? OnPost;

    /// <summary>
    /// Creates a new autoposter.
    /// </summary>
    /// <param name="client">The TOP.TL client</param>
    /// <param name="username">The listing username to post stats for</param>
    /// <param name="statsProvider">An async function that returns the current stats</param>
    public Autoposter(TopTLClient client, string username, Func<Task<StatsUpdate>> statsProvider)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _statsProvider = statsProvider ?? throw new ArgumentNullException(nameof(statsProvider));
    }

    /// <summary>
    /// Starts the autoposter with the given interval.
    /// </summary>
    /// <param name="interval">Duration between posts</param>
    public void Start(TimeSpan interval)
    {
        if (_timer != null)
            throw new InvalidOperationException("Autoposter is already running");

        _timer = new Timer(async _ => await PostAsync(), null, TimeSpan.Zero, interval);
    }

    /// <summary>
    /// Stops the autoposter.
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async Task PostAsync()
    {
        try
        {
            var stats = await _statsProvider();
            await _client.PostStatsAsync(_username, stats);
            OnPost?.Invoke(this, stats);
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, e);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

# TOP.TL C# SDK

[![NuGet](https://img.shields.io/nuget/v/TopTL.svg?color=004880&label=nuget)](https://www.nuget.org/packages/TopTL/)
[![Downloads](https://img.shields.io/nuget/dt/TopTL.svg?color=blue)](https://www.nuget.org/packages/TopTL/)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0-512bd4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/top-tl/csharp.svg?color=green)](https://github.com/top-tl/csharp/blob/main/LICENSE)
[![TOP.TL](https://img.shields.io/badge/top.tl-developers-2ec4b6)](https://top.tl/developers)

The official C# SDK for **TOP.TL** — post stats, check votes, and manage vote webhooks for your Telegram bot, channel, or group listed on [top.tl](https://top.tl).

Targets `net8.0` (LTS) and `net6.0`. No third-party runtime dependencies — uses only `System.Net.Http` and `System.Text.Json`.

## Install

```bash
dotnet add package TopTL
```

or via the NuGet Package Manager:

```
Install-Package TopTL
```

## Quickstart

Get an API key at https://top.tl/profile → **API Keys**. Scope the key to your listing and the operations you need (`listing:read`, `listing:write`, `votes:read`, `votes:check`).

```csharp
using TopTL;

using var client = new TopTLClient("toptl_xxx");

// Fetch a listing
var listing = await client.GetListingAsync("mybot");
Console.WriteLine($"{listing.Title} — {listing.VoteCount} votes");

// Post stats on a listing you own
await client.PostStatsAsync("mybot", new StatsPayload
{
    MemberCount = 5_000,
    GroupCount = 1_200,
    ChannelCount = 300,
});

// Reward users who voted
var check = await client.HasVotedAsync("mybot", 123456789);
if (check.Voted)
    GrantPremiumAccess(userId: 123456789);
```

## Dependency injection / `IHttpClientFactory`

Share an `HttpClient` managed by the framework rather than creating a new one per client:

```csharp
services.AddHttpClient<TopTLClient>();
// or
var http = httpClientFactory.CreateClient("toptl");
var client = new TopTLClient("toptl_xxx", http);
```

## Autoposter

For long-running bot processes, the SDK ships with a background autoposter that calls `PostStatsAsync` on an interval and, when `onlyOnChange: true`, only hits the API when the counters actually changed:

```csharp
using var client = new TopTLClient("toptl_xxx");

var poster = new Autoposter(
    client,
    "mybot",
    async ct => new StatsPayload { MemberCount = await bot.GetMemberCountAsync(ct) },
    interval: TimeSpan.FromMinutes(30),
    onlyOnChange: true);

poster.OnPost += (_, stats) => Console.WriteLine($"Posted: {stats.MemberCount} members");
poster.OnError += (_, err) => Console.WriteLine($"Autopost failed: {err.Message}");

poster.Start();

// On shutdown:
await poster.StopAsync();
```

For cron-style one-shots, skip the autoposter and call `client.PostStatsAsync(...)` directly.

## Vote webhooks

Register a URL TOP.TL will POST to whenever someone votes for your listing:

```csharp
await client.SetWebhookAsync(
    "mybot",
    url: "https://mybot.example.com/toptl-vote",
    rewardTitle: "30-day premium");   // shown to the voter

// Fire a synthetic test event to verify your endpoint
var result = await client.TestWebhookAsync("mybot");
Console.WriteLine($"{result.Success} — HTTP {result.StatusCode}");
```

The webhook payload contains the voting user (`userId`, `firstName`, `username`), the listing, and a timestamp.

## Batch stats

Post stats for up to 25 listings in a single request:

```csharp
var results = await client.BatchPostStatsAsync(new[]
{
    new BatchStatsItem { Username = "bot1", MemberCount = 1_200 },
    new BatchStatsItem { Username = "bot2", MemberCount = 5_400 },
});
```

## Global stats

```csharp
var stats = await client.GetGlobalStatsAsync();
Console.WriteLine($"{stats.Channels} channels, {stats.Groups} groups, {stats.Bots} bots");
```

## Error handling

Every API error raises a subclass of `TopTLException`:

```csharp
try
{
    await client.PostStatsAsync("mybot", new StatsPayload { MemberCount = 5_000 });
}
catch (TopTLAuthenticationException)   { /* bad key or missing scope */ }
catch (TopTLNotFoundException)         { /* listing does not exist */ }
catch (TopTLRateLimitException)        { /* back off and retry */ }
catch (TopTLValidationException ex)    { /* inspect ex.ResponseBody */ }
catch (TopTLException ex)              { /* transport / 5xx fallback */ }
```

## License

MIT — see [`LICENSE`](LICENSE).

# TOP.TL C# SDK

[![NuGet](https://img.shields.io/nuget/v/TopTL)](https://www.nuget.org/packages/TopTL)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 6+](https://img.shields.io/badge/.NET-6%2B-blue)](https://dotnet.microsoft.com/)

Official C# SDK for the [TOP.TL](https://top.tl) Telegram Directory API.

## Installation

### NuGet

```bash
dotnet add package TopTL
```

### Package Manager

```
Install-Package TopTL
```

## Quick Start

```csharp
using TopTL;

using var client = new TopTLClient("your-api-token");

// Get listing info
var listing = await client.GetListingAsync("mybot");
Console.WriteLine($"{listing.Title} has {listing.Votes} votes");

// Get votes
var votes = await client.GetVotesAsync("mybot");
Console.WriteLine($"Total votes: {votes.Total}");

// Check if a user voted
var check = await client.HasVotedAsync("mybot", "123456789");
if (check.HasVoted)
    Console.WriteLine("User has voted!");

// Post stats
await client.PostStatsAsync("mybot", new StatsUpdate(MemberCount: 50000));

// Get global stats
var stats = await client.GetStatsAsync();
Console.WriteLine($"Total listings on TOP.TL: {stats.TotalListings}");
```

## Autoposter

Automatically post stats at a regular interval:

```csharp
using TopTL;

using var client = new TopTLClient("your-api-token");

var autoposter = new Autoposter(client, "mybot", async () =>
    new StatsUpdate(MemberCount: await bot.GetMemberCountAsync()));

autoposter.OnPost += (_, stats) =>
    Console.WriteLine($"Posted stats: {stats}");

autoposter.OnError += (_, e) =>
    Console.WriteLine($"Failed to post stats: {e.Message}");

autoposter.Start(TimeSpan.FromMinutes(30));

// When shutting down:
autoposter.Stop();
```

## Requirements

- .NET 6 or higher
- No additional NuGet packages required (uses built-in `System.Net.Http` and `System.Text.Json`)

## License

[MIT](LICENSE) - TOP.TL

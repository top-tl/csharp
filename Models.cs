using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TopTL;

/// <summary>
/// A TOP.TL listing (bot, channel, or group).
/// </summary>
public record class Listing
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("username")]
    public string Username { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    /// <summary>One of <c>CHANNEL</c>, <c>GROUP</c>, <c>BOT</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("memberCount")]
    public long MemberCount { get; init; }

    [JsonPropertyName("voteCount")]
    public long VoteCount { get; init; }

    [JsonPropertyName("languages")]
    public List<string> Languages { get; init; } = new();

    [JsonPropertyName("verified")]
    public bool Verified { get; init; }

    [JsonPropertyName("featured")]
    public bool Featured { get; init; }

    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// A single recent voter on a listing.
/// </summary>
public record class Voter
{
    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "";

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("votedAt")]
    public string? VotedAt { get; init; }
}

/// <summary>
/// Result of <c>HasVotedAsync</c>.
/// </summary>
public record class VoteCheck
{
    [JsonPropertyName("voted")]
    public bool Voted { get; init; }

    [JsonPropertyName("votedAt")]
    public string? VotedAt { get; init; }
}

/// <summary>
/// Stats payload for <c>PostStatsAsync</c>. Only non-null fields are sent.
/// </summary>
public record class StatsPayload
{
    [JsonPropertyName("memberCount")]
    public int? MemberCount { get; init; }

    [JsonPropertyName("groupCount")]
    public int? GroupCount { get; init; }

    [JsonPropertyName("channelCount")]
    public int? ChannelCount { get; init; }

    /// <summary>Usernames of groups/channels the bot operates in.</summary>
    [JsonPropertyName("botServes")]
    public List<string>? BotServes { get; init; }
}

/// <summary>
/// Single item of a <c>BatchPostStatsAsync</c> request.
/// </summary>
public record class BatchStatsItem
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = "";

    [JsonPropertyName("memberCount")]
    public int? MemberCount { get; init; }

    [JsonPropertyName("groupCount")]
    public int? GroupCount { get; init; }

    [JsonPropertyName("channelCount")]
    public int? ChannelCount { get; init; }

    [JsonPropertyName("botServes")]
    public List<string>? BotServes { get; init; }
}

/// <summary>
/// Per-listing stats post result.
/// </summary>
public record class StatsResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Current webhook configuration for a listing.
/// </summary>
public record class WebhookConfig
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("rewardTitle")]
    public string? RewardTitle { get; init; }
}

/// <summary>
/// Result of <c>TestWebhookAsync</c>.
/// </summary>
public record class WebhookTestResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>
/// Site-wide TOP.TL counters.
/// </summary>
public record class GlobalStats
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("channels")]
    public int Channels { get; init; }

    [JsonPropertyName("groups")]
    public int Groups { get; init; }

    [JsonPropertyName("bots")]
    public int Bots { get; init; }
}

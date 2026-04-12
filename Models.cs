using System.Text.Json.Serialization;

namespace TopTL;

public record Listing(
    [property: JsonPropertyName("username")] string Username = "",
    [property: JsonPropertyName("title")] string Title = "",
    [property: JsonPropertyName("description")] string Description = "",
    [property: JsonPropertyName("category")] string Category = "",
    [property: JsonPropertyName("memberCount")] long? MemberCount = null,
    [property: JsonPropertyName("votes")] long Votes = 0,
    [property: JsonPropertyName("featured")] bool Featured = false,
    [property: JsonPropertyName("verified")] bool Verified = false,
    [property: JsonPropertyName("createdAt")] string CreatedAt = ""
);

public record VotesResponse(
    [property: JsonPropertyName("votes")] List<Vote> Votes = default!,
    [property: JsonPropertyName("total")] long Total = 0
)
{
    public VotesResponse() : this(new List<Vote>(), 0) { }
}

public record Vote(
    [property: JsonPropertyName("userId")] string UserId = "",
    [property: JsonPropertyName("timestamp")] string Timestamp = ""
);

public record VoteCheck(
    [property: JsonPropertyName("hasVoted")] bool HasVoted = false,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null
);

public record Stats(
    [property: JsonPropertyName("totalListings")] long TotalListings = 0,
    [property: JsonPropertyName("totalVotes")] long TotalVotes = 0,
    [property: JsonPropertyName("totalUsers")] long TotalUsers = 0
);

public record StatsUpdate(
    [property: JsonPropertyName("memberCount")] long? MemberCount = null,
    [property: JsonPropertyName("groupCount")] long? GroupCount = null
);

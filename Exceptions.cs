using System;

namespace TopTL;

/// <summary>Base for every exception this SDK raises.</summary>
public class TopTLException : Exception
{
    /// <summary>HTTP status returned by the server, when applicable.</summary>
    public int? StatusCode { get; }

    /// <summary>Raw response body (typically JSON text), when available.</summary>
    public string? ResponseBody { get; }

    public TopTLException(string message, int? statusCode = null, string? responseBody = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

/// <summary>401 / 403 — invalid or missing API key, or the key is missing the required scope.</summary>
public class TopTLAuthenticationException : TopTLException
{
    public TopTLAuthenticationException(string message, int? statusCode = null, string? responseBody = null)
        : base(message, statusCode, responseBody) { }
}

/// <summary>404 — the listing or resource does not exist.</summary>
public class TopTLNotFoundException : TopTLException
{
    public TopTLNotFoundException(string message, int? statusCode = null, string? responseBody = null)
        : base(message, statusCode, responseBody) { }
}

/// <summary>429 — API rate limit hit. Retry after a backoff.</summary>
public class TopTLRateLimitException : TopTLException
{
    public TopTLRateLimitException(string message, int? statusCode = null, string? responseBody = null)
        : base(message, statusCode, responseBody) { }
}

/// <summary>4xx — request payload was rejected by the server.</summary>
public class TopTLValidationException : TopTLException
{
    public TopTLValidationException(string message, int? statusCode = null, string? responseBody = null)
        : base(message, statusCode, responseBody) { }
}

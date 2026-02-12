using System;

namespace Aps.Core.Models;

/// <summary>
/// Exception thrown when token refresh fails.
/// </summary>
public class TokenRefreshException : Exception
{
    public TokenErrorResponse? ErrorResponse { get; }

    public TokenRefreshException(string message, TokenErrorResponse? errorResponse = null) : base(message)
    {
        ErrorResponse = errorResponse;
    }

    public TokenRefreshException(string message, Exception innerException, TokenErrorResponse? errorResponse = null)
        : base(message, innerException)
    {
        ErrorResponse = errorResponse;
    }
}

/// <summary>
/// Exception thrown when the refresh token has expired.
/// The user needs to re-authenticate.
/// </summary>
public class RefreshTokenExpiredException : TokenRefreshException
{
    public RefreshTokenExpiredException(string message, TokenErrorResponse? errorResponse = null)
        : base(message, errorResponse)
    {
    }

    public RefreshTokenExpiredException(string message, Exception innerException, TokenErrorResponse? errorResponse = null)
        : base(message, innerException, errorResponse)
    {
    }
}

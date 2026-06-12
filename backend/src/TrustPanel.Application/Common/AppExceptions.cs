namespace TrustPanel.Application.Common;

/// <summary>Maps to a 404 envelope response.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Maps to a 409 envelope response.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Maps to a 401 envelope response.</summary>
public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>Maps to a 403 envelope response.</summary>
public class ForbiddenAppException : Exception
{
    public ForbiddenAppException(string message) : base(message) { }
}

/// <summary>Maps to a 429 envelope response.</summary>
public class RateLimitedException : Exception
{
    public RateLimitedException(string message) : base(message) { }
}

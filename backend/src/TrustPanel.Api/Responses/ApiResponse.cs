namespace TrustPanel.Api.Responses;

public sealed record ApiResponse<T>(
    int Code,
    bool Status,
    T? Data,
    string Message,
    string Error,
    IReadOnlyDictionary<string, string[]> Errors);

public static class ApiResults
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>();

    private static readonly object EmptyData = new { };

    public static IResult Ok<T>(T data, string message = "Request completed successfully.")
    {
        return Envelope(StatusCodes.Status200OK, true, data, message, string.Empty, EmptyErrors);
    }

    public static IResult Created<T>(T data, string message = "Resource created successfully.")
    {
        return Envelope(StatusCodes.Status201Created, true, data, message, string.Empty, EmptyErrors);
    }

    public static IResult NoContent(string message = "Request completed successfully.")
    {
        return Envelope(StatusCodes.Status200OK, true, EmptyData, message, string.Empty, EmptyErrors);
    }

    public static IResult ValidationError(
        IReadOnlyDictionary<string, string[]> errors,
        string message = "Validation failed.")
    {
        return Envelope(StatusCodes.Status400BadRequest, false, EmptyData, message, "Validation failed.", errors);
    }

    public static IResult Unauthorized(string message = "Authentication is required.")
    {
        return Envelope(StatusCodes.Status401Unauthorized, false, EmptyData, message, "Unauthorized.", EmptyErrors);
    }

    public static IResult Forbidden(string message = "You do not have permission to perform this action.")
    {
        return Envelope(StatusCodes.Status403Forbidden, false, EmptyData, message, "Forbidden.", EmptyErrors);
    }

    public static IResult NotFound(string message = "The requested resource was not found.")
    {
        return Envelope(StatusCodes.Status404NotFound, false, EmptyData, message, "Not found.", EmptyErrors);
    }

    public static IResult Conflict(string message = "The request conflicts with the current resource state.")
    {
        return Envelope(StatusCodes.Status409Conflict, false, EmptyData, message, "Conflict.", EmptyErrors);
    }

    public static IResult RateLimited(string message = "Too many requests. Please try again later.")
    {
        return Envelope(StatusCodes.Status429TooManyRequests, false, EmptyData, message, "Rate limit exceeded.", EmptyErrors);
    }

    public static IResult ServerError(string message = "An unexpected error occurred.")
    {
        return Envelope(StatusCodes.Status500InternalServerError, false, EmptyData, message, "Internal server error.", EmptyErrors);
    }

    public static IResult Envelope<T>(
        int code,
        bool status,
        T data,
        string message,
        string error,
        IReadOnlyDictionary<string, string[]> errors)
    {
        var response = new ApiResponse<T>(code, status, data, message, error, errors);
        return Results.Json(response, statusCode: code);
    }
}

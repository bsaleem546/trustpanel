using FluentValidation;
using TrustPanel.Api.Responses;
using TrustPanel.Application.Common;

namespace TrustPanel.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            await ToResult(exception).ExecuteAsync(context);
        }
    }

    private IResult ToResult(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validationException:
                var errors = validationException.Errors
                    .GroupBy(e => string.IsNullOrEmpty(e.PropertyName)
                        ? e.PropertyName
                        : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                return ApiResults.ValidationError(errors);
            case UnauthorizedAppException unauthorized:
                return ApiResults.Unauthorized(unauthorized.Message);
            case ForbiddenAppException forbidden:
                return ApiResults.Forbidden(forbidden.Message);
            case NotFoundException notFound:
                return ApiResults.NotFound(notFound.Message);
            case ConflictException conflict:
                return ApiResults.Conflict(conflict.Message);
            default:
                _logger.LogError(exception, "Unhandled API exception.");
                return ApiResults.ServerError();
        }
    }
}

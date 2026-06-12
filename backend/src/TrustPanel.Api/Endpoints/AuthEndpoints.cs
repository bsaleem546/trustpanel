using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Auth.Commands;

namespace TrustPanel.Api.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "tp_refresh";
    private const string ExternalCookieScheme = "External";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new RegisterCommand(request.Email, request.Password, request.WorkspaceName));
            return ApiResults.Created(
                new { userId = result.UserId, workspaceId = result.WorkspaceId },
                "Account created. Check your email to confirm your address.");
        });

        group.MapPost("/confirm-email", async (ConfirmEmailRequest request, IMediator mediator) =>
        {
            await mediator.Send(new ConfirmEmailCommand(request.UserId, request.Token));
            return ApiResults.NoContent("Email confirmed. You can now sign in.");
        });

        group.MapPost("/login", async (LoginRequest request, HttpContext context, IMediator mediator) =>
        {
            var result = await mediator.Send(new LoginCommand(
                request.Email, request.Password, UserAgent(context), IpAddress(context)));
            SetRefreshCookie(context, result);
            return ApiResults.Ok(ToAuthPayload(result), "Signed in successfully.");
        });

        group.MapPost("/refresh", async (HttpContext context, IMediator mediator) =>
        {
            var refreshToken = context.Request.Cookies[RefreshTokenCookieName];
            var result = await mediator.Send(new RefreshTokenCommand(
                refreshToken ?? string.Empty, UserAgent(context), IpAddress(context)));
            SetRefreshCookie(context, result);
            return ApiResults.Ok(ToAuthPayload(result), "Token refreshed.");
        });

        group.MapPost("/logout", async (HttpContext context, IMediator mediator) =>
        {
            await mediator.Send(new LogoutCommand(context.Request.Cookies[RefreshTokenCookieName]));
            DeleteRefreshCookie(context);
            return ApiResults.NoContent("Signed out.");
        });

        group.MapGet("/sessions", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var sessions = await mediator.Send(
                new ListSessionsQuery(user.GetUserId(), user.GetSessionId()));
            return ApiResults.Ok(new { items = sessions, total = sessions.Count }, "Active sessions.");
        }).RequireAuthorization();

        group.MapDelete("/sessions/{sessionId:guid}",
            async (Guid sessionId, ClaimsPrincipal user, IMediator mediator) =>
            {
                await mediator.Send(new RevokeSessionCommand(user.GetUserId(), sessionId));
                return ApiResults.NoContent("Session revoked.");
            }).RequireAuthorization();

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, IMediator mediator) =>
        {
            await mediator.Send(new ForgotPasswordCommand(request.Email));
            return ApiResults.NoContent(
                "If an account exists for that email, a password reset link has been sent.");
        });

        group.MapPost("/reset-password", async (ResetPasswordRequest request, IMediator mediator) =>
        {
            await mediator.Send(new ResetPasswordCommand(
                request.Email, request.Token, request.NewPassword));
            return ApiResults.NoContent("Password updated. Sign in with your new password.");
        });

        group.MapGet("/me", async (ClaimsPrincipal user, IIdentityService identityService) =>
        {
            var me = await identityService.FindByIdAsync(user.GetUserId());
            return me is null
                ? ApiResults.NotFound("User not found.")
                : ApiResults.Ok(new
                {
                    id = me.Id,
                    email = me.Email,
                    role = me.Role,
                    onboardingCompleted = me.OnboardingCompleted,
                    workspaceId = user.GetWorkspaceId()
                }, "Current user.");
        }).RequireAuthorization();

        group.MapPut("/onboarding",
            async (OnboardingRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var state = await mediator.Send(new UpdateOnboardingCommand(
                    user.GetUserId(),
                    request.WorkspaceName,
                    request.LogoPath,
                    request.FirstFormTemplate,
                    request.EmbedSnippetViewed,
                    request.Completed ?? false));
                return ApiResults.Ok(state, "Onboarding progress saved.");
            }).RequireAuthorization();

        group.MapGet("/google", (HttpContext context, IConfiguration configuration) =>
        {
            if (string.IsNullOrWhiteSpace(configuration["GOOGLE_CLIENT_ID"]))
            {
                return ApiResults.NotFound("Google sign-in is not configured.");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/google/callback"
            };
            return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        });

        group.MapGet("/google/callback", async (HttpContext context, IMediator mediator) =>
        {
            var auth = await context.AuthenticateAsync(ExternalCookieScheme);
            var email = auth.Principal?.FindFirstValue(ClaimTypes.Email);
            if (!auth.Succeeded || string.IsNullOrEmpty(email))
            {
                return ApiResults.Unauthorized("Google sign-in failed.");
            }

            var result = await mediator.Send(new ExternalLoginCommand(
                email, UserAgent(context), IpAddress(context)));
            SetRefreshCookie(context, result);
            await context.SignOutAsync(ExternalCookieScheme);

            var frontendBaseUrl = context.RequestServices
                .GetRequiredService<IConfiguration>()["FRONTEND_BASE_URL"] ?? "/";
            return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth/callback");
        });
    }

    private static object ToAuthPayload(AuthResultDto result) => new
    {
        accessToken = result.AccessToken.Token,
        accessTokenExpiresAt = result.AccessToken.ExpiresAt,
        sessionId = result.SessionId,
        workspaceId = result.WorkspaceId,
        user = new
        {
            id = result.User.Id,
            email = result.User.Email,
            role = result.User.Role,
            onboardingCompleted = result.User.OnboardingCompleted
        }
    };

    private static void SetRefreshCookie(HttpContext context, AuthResultDto result)
    {
        context.Response.Cookies.Append(RefreshTokenCookieName, result.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/api/auth",
                Expires = result.RefreshTokenExpiresAt
            });
    }

    private static void DeleteRefreshCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        });
    }

    private static string UserAgent(HttpContext context)
        => context.Request.Headers.UserAgent.ToString() is { Length: > 0 } agent
            ? agent[..Math.Min(agent.Length, 512)]
            : "unknown";

    private static string IpAddress(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private sealed record RegisterRequest(string Email, string Password, string? WorkspaceName);
    private sealed record ConfirmEmailRequest(Guid UserId, string Token);
    private sealed record LoginRequest(string Email, string Password);
    private sealed record ForgotPasswordRequest(string Email);
    private sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
    private sealed record OnboardingRequest(
        string? WorkspaceName,
        string? LogoPath,
        string? FirstFormTemplate,
        bool? EmbedSnippetViewed,
        bool? Completed);
}

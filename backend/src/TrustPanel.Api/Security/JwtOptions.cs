using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrustPanel.Infrastructure.Security;

namespace TrustPanel.Api.Security;

public sealed class JwtOptions
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SigningKey { get; init; }

    public static JwtOptions From(IConfiguration configuration) => new()
    {
        Issuer = configuration["JWT_ISSUER"] ?? "trustpanel",
        Audience = configuration["JWT_AUDIENCE"] ?? "trustpanel-api",
        SigningKey = string.IsNullOrWhiteSpace(configuration["JWT_SIGNING_KEY"])
            ? JwtTokenService.DevelopmentSigningKey
            : configuration["JWT_SIGNING_KEY"]!
    };

    public TokenValidationParameters ToTokenValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
}

# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TrustPanel.sln .
COPY src/TrustPanel.Domain/TrustPanel.Domain.csproj            src/TrustPanel.Domain/
COPY src/TrustPanel.Application/TrustPanel.Application.csproj  src/TrustPanel.Application/
COPY src/TrustPanel.Infrastructure/TrustPanel.Infrastructure.csproj src/TrustPanel.Infrastructure/
COPY src/TrustPanel.Api/TrustPanel.Api.csproj                  src/TrustPanel.Api/

RUN dotnet restore src/TrustPanel.Api/TrustPanel.Api.csproj

COPY . .
RUN dotnet publish src/TrustPanel.Api/TrustPanel.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for container security
RUN addgroup --system --gid 1001 trustpanel \
 && adduser  --system --uid 1001 --ingroup trustpanel trustpanel

COPY --from=build --chown=trustpanel:trustpanel /app/publish .

USER trustpanel

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TrustPanel.Api.dll"]

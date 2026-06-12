# TrustPanel Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete TrustPanel ASP.NET Core backend described in `trustpanel-blueprint-dotnet.html`: multi-tenant testimonial collection, management, widgets, billing, analytics, email, AI jobs, agency white-label, public API, and super-admin operations.

**Architecture:** Use Clean Architecture with `TrustPanel.Domain` free of framework dependencies, `TrustPanel.Application` containing MediatR commands/queries and contracts, `TrustPanel.Infrastructure` owning EF Core and external services, and `TrustPanel.Api` exposing Minimal API route groups. PostgreSQL is the source of truth, Redis supports cache/rate limits/jobs, and Hangfire runs all slow work outside request paths.

**Tech Stack:** .NET 8, ASP.NET Core Minimal API, ASP.NET Core Identity, JWT bearer auth, EF Core 8, PostgreSQL 15, Redis, MediatR, FluentValidation, Hangfire, Stripe.net, Resend, Cloudflare R2 via AWSSDK.S3, Cloudflare Turnstile, Meilisearch, Anthropic .NET SDK, Scalar/Swashbuckle, xUnit, WebApplicationFactory, Testcontainers.

---

## Backend Scope

Build the backend for these product modules:

1. Auth and onboarding
2. Workspace and white-label
3. Testimonial collection forms
4. Testimonial management dashboard
5. AI features
6. Widget builder and public widget data
7. Analytics
8. Email system
9. Team and permissions
10. Public REST API
11. Billing and subscriptions
12. Super admin panel

The separate React dashboard, public collection UI, and vanilla TypeScript widget are not implemented in this backend plan, but backend endpoints must support them.

## Target Solution Layout

Create this solution structure:

```text
TrustPanel.sln
src/
  TrustPanel.Api/
    Program.cs
    Endpoints/
    Middleware/
    Security/
    OpenApi/
  TrustPanel.Application/
    Common/
    Auth/
    Workspaces/
    Forms/
    Testimonials/
    Widgets/
    Analytics/
    Email/
    Billing/
    Teams/
    PublicApi/
    Admin/
  TrustPanel.Domain/
    Common/
    Users/
    Workspaces/
    Forms/
    Testimonials/
    Widgets/
    Analytics/
    Email/
    Billing/
    Teams/
    Integrations/
  TrustPanel.Infrastructure/
    Persistence/
    Identity/
    Jobs/
    Email/
    Billing/
    Storage/
    Search/
    Ai/
    Security/
    Integrations/
tests/
  TrustPanel.UnitTests/
  TrustPanel.IntegrationTests/
```

## Core Domain Model

Implement these entities and value objects before feature endpoints:

| Entity | Required fields |
| --- | --- |
| `ApplicationUser` | `Id`, `Email`, `PlanId`, `StripeCustomerId`, `Role`, Identity fields |
| `RefreshToken` | `Id`, `UserId`, `TokenHash`, `SessionId`, `UserAgent`, `IpAddress`, `ExpiresAt`, `RevokedAt`, `CreatedAt` |
| `Plan` | `Id`, `Code`, `Name`, `MonthlyPrice`, `AnnualPrice`, `WorkspaceLimit`, `TestimonialLimit`, `WidgetLimit`, feature booleans |
| `Subscription` | `Id`, `UserId`, `StripeSubscriptionId`, `StripeCustomerId`, `PlanId`, `Status`, `CurrentPeriodEnd`, `CancelAtPeriodEnd`, `GracePeriodEndsAt` |
| `Workspace` | `Id`, `OwnerUserId`, `Slug`, `Name`, `CustomDomain`, `DomainVerifiedAt`, `Branding`, `EmailFrom`, timestamps |
| `WorkspaceMember` | `Id`, `WorkspaceId`, `UserId`, `Role`, `InvitedEmail`, `InvitationTokenHash`, `InvitationExpiresAt`, `AcceptedAt` |
| `CollectionForm` | `Id`, `WorkspaceId`, `Slug`, `Name`, `AllowedSubmissionType`, `QuestionConfig`, `ThankYouConfig`, `RewardConfig`, `IsActive` |
| `Testimonial` | `Id`, `WorkspaceId`, `CollectionFormId`, `Type`, `Content`, `VideoPath`, `ThumbnailPath`, `Rating`, `Status`, `Source`, `Submitter`, `SentimentScore`, `Highlight`, `Tags`, `FeaturedAt`, timestamps |
| `Widget` | `Id`, `WorkspaceId`, `Type`, `Name`, `FilterTags`, `MinimumRating`, `FeaturedOnly`, `SelectedTestimonialIds`, `SourceFilter`, `Settings`, `CustomCss`, timestamps |
| `WidgetEvent` | `Id`, `WidgetId`, `TestimonialId`, `Event`, `Country`, `Device`, `Referrer`, `OccurredAt` |
| `WidgetAnalyticsDaily` | `Id`, `WidgetId`, `Date`, `Views`, `Clicks`, `UniqueVisitors`, `TopCountry`, `TopDevice` |
| `EmailLog` | `Id`, `WorkspaceId`, `Template`, `Recipient`, `ProviderMessageId`, `Status`, `SentAt`, `DeliveredAt`, `FailedAt`, `Error` |
| `EmailSuppression` | `Id`, `WorkspaceId`, `Email`, `Reason`, `CreatedAt` |
| `ApiKey` | `Id`, `WorkspaceId`, `Name`, `KeyHash`, `Prefix`, `RevokedAt`, `LastUsedAt`, `CreatedAt` |
| `WebhookEndpoint` | `Id`, `WorkspaceId`, `Url`, `Secret`, `IsActive`, timestamps |
| `ImportSource` | `Id`, `WorkspaceId`, `Provider`, `ExternalAccountId`, `Config`, `LastSyncedAt`, `IsActive` |
| `AuditLog` | `Id`, `WorkspaceId`, `ActorUserId`, `Action`, `EntityType`, `EntityId`, `Metadata`, `CreatedAt` |
| `SuperAdminOverride` | `Id`, `UserId`, `PlanId`, `Reason`, `ExpiresAt`, `CreatedByUserId`, `CreatedAt` |

Use EF Core owned JSONB types for `Branding`, `EmailFrom`, `QuestionConfig`, `ThankYouConfig`, `RewardConfig`, `Submitter`, and `WidgetSettings`.

## API Route Groups

Use these route groups consistently:

```text
/api/auth/*
/api/workspaces/*
/api/forms/*
/api/testimonials/*
/api/widgets/*
/api/analytics/*
/api/email/*
/api/billing/*
/api/team/*
/api/v1/*
/api/public/*
/api/admin/*
/webhooks/stripe
/webhooks/resend
/hangfire
```

Apply authentication, authorization, workspace resolution, plan limit checks, and rate limits at route-group level wherever possible.

## API Response Architecture

Every API endpoint built for this system must return the same JSON response envelope, regardless of success, validation failure, authorization failure, not-found result, rate limit, or server error. This applies to dashboard APIs, public form APIs, public widget APIs, public REST API v1, admin APIs, auth APIs, billing APIs, upload APIs, webhook endpoints, and any future frontend-facing endpoint.

Required response shape:

```json
{
  "code": 200,
  "status": true,
  "data": {},
  "message": "",
  "error": "",
  "errors": {}
}
```

Rules:

- `code` must match the HTTP status code returned by the API.
- `status` must be `true` only for 2xx responses.
- `data` must contain the response payload for successful responses. Use `{}` when there is no payload.
- `message` must contain a human-readable success or failure message.
- `error` must contain a single error summary string for failed responses. Use `""` for successful responses.
- `errors` must contain field-level validation errors or structured error details. Use `{}` when there are no field errors.
- Lists must be wrapped inside `data`, for example `{ "data": { "items": [], "total": 0 } }`.
- Pagination metadata must live inside `data`, not beside the envelope.
- Exceptions must be converted by global exception middleware into this same envelope.
- FluentValidation errors must be converted into `errors` as a property-name-to-string-array object.
- Authentication, authorization, not-found, conflict, rate-limit, Stripe webhook, Resend webhook, and Turnstile failures must all use this envelope.

Implementation requirement:

```csharp
public sealed record ApiResponse<T>(
    int Code,
    bool Status,
    T? Data,
    string Message,
    string Error,
    IReadOnlyDictionary<string, string[]> Errors);
```

Create response helpers in `src/TrustPanel.Api/Responses/ApiResponse.cs` and require Minimal API handlers to return these helpers instead of anonymous objects or raw DTOs.

## Environment Configuration

All required backend connections, API keys, signing secrets, provider credentials, webhook secrets, and operational settings must be documented in `.env.example`. The `.env.example` file must contain placeholder values only; real secrets must never be committed. The actual `.env` file will be created later from `.env.example`.

Required `.env.example` entries:

```env
# App
ASPNETCORE_ENVIRONMENT=Development
APP_BASE_URL=http://localhost:5000
FRONTEND_BASE_URL=http://localhost:5173
PUBLIC_WIDGET_CDN_URL=https://cdn.trustpanel.com/widget.js
ALLOWED_CORS_ORIGINS=http://localhost:5173

# Database
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=trustpanel
POSTGRES_USER=trustpanel
POSTGRES_PASSWORD=change-me
CONNECTIONSTRINGS__DEFAULT=Host=localhost;Port=5432;Database=trustpanel;Username=trustpanel;Password=change-me

# Redis
REDIS_CONNECTION=localhost:6379
REDIS_INSTANCE_NAME=trustpanel

# Authentication and tokens
JWT_ISSUER=trustpanel
JWT_AUDIENCE=trustpanel-api
JWT_SIGNING_KEY=replace-with-long-random-secret
ACCESS_TOKEN_MINUTES=15
REFRESH_TOKEN_DAYS=30
DATA_PROTECTION_KEYS_PATH=/var/trustpanel/keys

# Google OAuth
GOOGLE_CLIENT_ID=replace-me
GOOGLE_CLIENT_SECRET=replace-me

# Stripe
STRIPE_SECRET_KEY=sk_test_replace_me
STRIPE_WEBHOOK_SECRET=whsec_replace_me
STRIPE_PRICE_STARTER_MONTHLY=price_replace_me
STRIPE_PRICE_STARTER_ANNUAL=price_replace_me
STRIPE_PRICE_PRO_MONTHLY=price_replace_me
STRIPE_PRICE_PRO_ANNUAL=price_replace_me
STRIPE_PRICE_AGENCY_MONTHLY=price_replace_me
STRIPE_PRICE_AGENCY_ANNUAL=price_replace_me
STRIPE_PRICE_AGENCY_PLUS_MONTHLY=price_replace_me
STRIPE_PRICE_AGENCY_PLUS_ANNUAL=price_replace_me

# Resend email
RESEND_API_KEY=re_replace_me
RESEND_WEBHOOK_SECRET=replace-me
DEFAULT_EMAIL_FROM=noreply@trustpanel.com

# Cloudflare R2 / S3-compatible storage
R2_ACCOUNT_ID=replace-me
R2_ACCESS_KEY_ID=replace-me
R2_SECRET_ACCESS_KEY=replace-me
R2_BUCKET_NAME=trustpanel
R2_PUBLIC_ENDPOINT=https://replace-me.r2.cloudflarestorage.com
R2_PRESIGNED_UPLOAD_TTL_MINUTES=15
R2_PRESIGNED_READ_TTL_MINUTES=10
MAX_VIDEO_UPLOAD_MB=250

# Cloudflare Turnstile
TURNSTILE_SECRET_KEY=replace-me

# Cloudflare custom domains / CDN
CLOUDFLARE_API_TOKEN=replace-me
CLOUDFLARE_ZONE_ID=replace-me
CLOUDFLARE_ACCOUNT_ID=replace-me
CUSTOM_DOMAIN_CNAME_TARGET=domains.trustpanel.com

# Hangfire
HANGFIRE_STORAGE=postgres
HANGFIRE_DASHBOARD_PATH=/hangfire
HANGFIRE_WORKER_COUNT=5

# Meilisearch
MEILISEARCH_URL=http://localhost:7700
MEILISEARCH_MASTER_KEY=replace-me
MEILISEARCH_TESTIMONIAL_INDEX=testimonials

# Anthropic / Claude
ANTHROPIC_API_KEY=replace-me
ANTHROPIC_SENTIMENT_MODEL=claude-haiku-3-5
ANTHROPIC_INSIGHTS_MODEL=claude-sonnet-4-6

# Sentry
SENTRY_DSN=
SENTRY_RELEASE=trustpanel-api-local

# Security and limits
API_KEY_PEPPER=replace-with-long-random-secret
SUBMISSION_RATE_LIMIT_PER_HOUR=5
PUBLIC_API_RATE_LIMIT_PER_HOUR=1000
EMAIL_LIMIT_PER_ADDRESS_30_DAYS=3
PAYMENT_FAILURE_GRACE_DAYS=3

# Super admin bootstrap
SUPER_ADMIN_EMAIL=admin@example.com
SUPER_ADMIN_TEMP_PASSWORD=change-me
```

Any new provider, database, queue, cache, signing key, webhook secret, OAuth credential, or deployment setting added later must be added to `.env.example` in the same change as the code that uses it.

## Implementation Phases

### Phase 0: Foundation

**Files:**
- Create: `TrustPanel.sln`
- Create: `src/TrustPanel.Api/TrustPanel.Api.csproj`
- Create: `src/TrustPanel.Application/TrustPanel.Application.csproj`
- Create: `src/TrustPanel.Domain/TrustPanel.Domain.csproj`
- Create: `src/TrustPanel.Infrastructure/TrustPanel.Infrastructure.csproj`
- Create: `tests/TrustPanel.UnitTests/TrustPanel.UnitTests.csproj`
- Create: `tests/TrustPanel.IntegrationTests/TrustPanel.IntegrationTests.csproj`
- Create: `src/TrustPanel.Api/Responses/ApiResponse.cs`
- Create: `src/TrustPanel.Api/Middleware/ApiExceptionMiddleware.cs`
- Create: `docker-compose.yml`
- Create: `.env.example`

- [x] Create the .NET 8 solution and projects.
- [x] Reference projects in Clean Architecture direction: API -> Application + Infrastructure, Infrastructure -> Application + Domain, Application -> Domain.
- [x] Add NuGet packages: ASP.NET Core Identity EF, Npgsql EF Core provider, MediatR, FluentValidation, Hangfire, StackExchange.Redis, Stripe.net, Resend, AWSSDK.S3, Meilisearch, Swashbuckle/Scalar, xUnit, FluentAssertions, Testcontainers.
- [x] Create `.env.example` with every required connection string, API key, webhook secret, signing secret, rate-limit value, storage setting, OAuth credential, and admin bootstrap value listed in the Environment Configuration section.
- [x] Load configuration from environment variables and fail fast at startup when required production settings are missing.
- [x] Implement `ApiResponse<T>` and response helper methods for success, created, no-content, validation error, unauthorized, forbidden, not found, conflict, rate limit, and server error responses.
- [x] Implement global exception middleware that converts unhandled exceptions into the required response envelope with `code`, `status`, `data`, `message`, `error`, and `errors`.
- [x] Add an integration test that calls one successful endpoint and one failing endpoint and asserts both responses use the exact required envelope keys.
- [x] Add local Docker services for PostgreSQL, Redis, Meilisearch, and an S3-compatible storage emulator for development.
- [x] Add health checks for PostgreSQL, Redis, Meilisearch, R2/S3 configuration, and Hangfire storage.
- [x] Add structured logging, request correlation IDs, exception mapping, and Sentry configuration hooks.
- [x] Verify with `dotnet build` and an integration smoke test using WebApplicationFactory.

### Phase 1: Persistence and Multi-Tenant Core

**Files:**
- Create: `src/TrustPanel.Domain/**`
- Create: `src/TrustPanel.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/TrustPanel.Infrastructure/Persistence/Configurations/*.cs`
- Create: `src/TrustPanel.Infrastructure/Persistence/Migrations/*`

- [x] Implement all core entities, enums, value objects, and timestamps.
- [x] Configure EF Core table mappings, indexes, unique constraints, and JSONB owned types.
- [x] Configure global query filters for workspace-scoped entities: `Testimonial`, `Widget`, `CollectionForm`, `WidgetEvent`, `EmailLog`, `ApiKey`, `WebhookEndpoint`, `ImportSource`, and `AuditLog`.
- [x] Add `ICurrentUser` and `ICurrentWorkspace` abstractions in Application.
- [x] Add workspace resolution middleware that reads workspace from authenticated claims, route values, or custom domain host.
- [x] Seed the four plans: Starter, Pro, Agency, Agency+.
- [x] Add migrations and verify schema creation against PostgreSQL.
- [x] Add tests proving workspace data cannot leak across tenants.

### Phase 2: Auth and Onboarding

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/AuthEndpoints.cs`
- Create: `src/TrustPanel.Application/Auth/**`
- Create: `src/TrustPanel.Infrastructure/Identity/**`
- Create: `src/TrustPanel.Api/Security/JwtOptions.cs`

- [x] Implement registration with email/password through ASP.NET Core Identity.
- [x] Send email confirmation token on registration.
- [x] Implement login with 15-minute JWT access token and 30-day refresh token in `httpOnly`, `Secure`, `SameSite` cookie.
- [x] Hash refresh tokens in database and expose active session listing by `SessionId`.
- [x] Implement refresh, logout current session, and revoke session by ID.
- [x] Implement password reset with `DataProtectionTokenProvider` and 60-minute expiry.
- [x] Add Google OAuth using ASP.NET Core OAuth middleware.
- [x] Create a default workspace during onboarding.
- [x] Persist onboarding state: workspace name, logo, first form template, and embed snippet viewed.
- [x] Add auth integration tests for register, confirm email, login, refresh, revoke, and password reset.

### Phase 3: Workspaces, Branding, Custom Domains

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/WorkspaceEndpoints.cs`
- Create: `src/TrustPanel.Application/Workspaces/**`
- Create: `src/TrustPanel.Infrastructure/Jobs/VerifyWorkspaceDomainJob.cs`

- [x] Implement CRUD for workspaces within plan limits.
- [x] Implement white-label branding updates: logo, colors, font, TrustPanel branding visibility, and email sender.
- [x] Enforce Agency plan for white-label removal and custom domains.
- [x] Implement custom domain save flow with required CNAME target.
- [x] Add middleware that resolves workspace from `Host` for public form/widget requests.
- [x] Add Hangfire recurring DNS verification job that sets `DomainVerifiedAt`.
- [x] Add tests for plan gates, host-based workspace resolution, and DNS verification state transitions.

### Phase 4: Collection Forms and Public Submissions

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/FormEndpoints.cs`
- Create: `src/TrustPanel.Api/Endpoints/PublicFormEndpoints.cs`
- Create: `src/TrustPanel.Application/Forms/**`
- Create: `src/TrustPanel.Infrastructure/Security/TurnstileClient.cs`

- [ ] Implement collection form builder APIs for question toggles, allowed submission type, thank-you config, redirect URL, and reward config.
- [ ] Implement public form read endpoint by workspace slug/custom domain and form slug.
- [ ] Implement text testimonial submission via `SubmitTestimonialCommand`.
- [ ] Validate Cloudflare Turnstile token server-side before accepting public submissions.
- [ ] Apply rate limit: 5 submissions per IP per hour per form using Redis token bucket.
- [ ] Dispatch Hangfire jobs after submission: thank-you email, owner notification, sentiment analysis when enabled.
- [ ] Implement auto-approve rule: rating >= 4 and sentiment score > 0.4 after AI job completes.
- [ ] Add CSP headers on public collection form endpoints.
- [ ] Add integration tests for form read, valid submission, Turnstile failure, and rate limit failure.

### Phase 5: Testimonial Management

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/TestimonialEndpoints.cs`
- Create: `src/TrustPanel.Application/Testimonials/**`
- Create: `src/TrustPanel.Infrastructure/Search/MeilisearchTestimonialIndexer.cs`
- Create: `src/TrustPanel.Infrastructure/Persistence/Interceptors/SearchIndexSaveChangesInterceptor.cs`
- Create: `src/TrustPanel.Application/Common/Behaviors/AuditLogPostProcessor.cs`

- [ ] Implement inbox query for pending testimonials.
- [ ] Implement approve, reject, feature, unfeature, tag, untag, delete, and edit commands.
- [ ] Implement batch endpoints for bulk approve/reject/feature/tag/delete.
- [ ] Record content edits with `EditedAt` and immutable `AuditLog` entries.
- [ ] Implement CSV import as a Hangfire job.
- [ ] Implement import source model for Twitter/X, Google Business Profile, G2, Trustpilot, and CSV.
- [ ] Add Meilisearch indexing through EF Core SaveChanges interceptor.
- [ ] Add full-text testimonial search endpoint.
- [ ] Add tests for moderation transitions, bulk commands, audit logs, and search indexing dispatch.

### Phase 6: Video Upload and Processing

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/UploadEndpoints.cs`
- Create: `src/TrustPanel.Infrastructure/Storage/R2StorageService.cs`
- Create: `src/TrustPanel.Infrastructure/Jobs/ProcessVideoJob.cs`

- [ ] Implement pre-signed Cloudflare R2 upload URL creation.
- [ ] Enforce server-side MIME allowlist and max upload size before issuing upload URLs.
- [ ] Store only private R2 object keys in the database.
- [ ] Implement pre-signed read URLs with short TTL.
- [ ] Implement Hangfire FFmpeg job for trim, compression, and thumbnail generation.
- [ ] Ensure video bytes never route through Kestrel.
- [ ] Add tests for upload policy validation, private URL generation, and job enqueueing.

### Phase 7: Widget Builder and Public Widget Data

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/WidgetEndpoints.cs`
- Create: `src/TrustPanel.Api/Endpoints/PublicWidgetEndpoints.cs`
- Create: `src/TrustPanel.Application/Widgets/**`

- [ ] Implement widget CRUD with six types: carousel, masonry grid/wall of love, badge, popup, slider, single featured card.
- [ ] Implement widget settings for card style, colors, font size, animation, and dark mode.
- [ ] Implement filters: tags, minimum rating, featured-only, selected testimonial IDs, and source platform.
- [ ] Implement custom CSS field.
- [ ] Implement public endpoint `GET /api/public/widget/{id}` returning the required API response envelope with minimal approved testimonial data inside `data`.
- [ ] Cache widget public response in Redis and set CDN-friendly headers for 60-second Cloudflare TTL.
- [ ] Bust widget cache when widget settings or related approved testimonials change.
- [ ] Add tests for filtering, tenant isolation, public payload shape, and cache invalidation.

### Phase 8: Billing and Plan Enforcement

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/BillingEndpoints.cs`
- Create: `src/TrustPanel.Api/Endpoints/StripeWebhookEndpoints.cs`
- Create: `src/TrustPanel.Application/Billing/**`
- Create: `src/TrustPanel.Infrastructure/Billing/StripeBillingService.cs`
- Create: `src/TrustPanel.Api/Middleware/CheckWorkspaceLimitsMiddleware.cs`

- [ ] Implement checkout session creation for Starter, Pro, Agency, and Agency+ monthly/annual prices.
- [ ] Implement Stripe customer portal session creation.
- [ ] Validate `Stripe-Signature` before processing webhook payloads.
- [ ] Handle `invoice.payment_succeeded`, `invoice.payment_failed`, `customer.subscription.updated`, and `customer.subscription.deleted`.
- [ ] Apply 3-day grace period after payment failure.
- [ ] Implement plan limit middleware for write endpoints.
- [ ] Enforce downgrade behavior: keep existing data, block new creates until under limit.
- [ ] Add tests for webhook signature validation, plan transitions, grace period, and limit blocking.

### Phase 9: Email System

**Files:**
- Create: `src/TrustPanel.Application/Email/**`
- Create: `src/TrustPanel.Infrastructure/Email/ResendEmailSender.cs`
- Create: `src/TrustPanel.Infrastructure/Email/RazorLightTemplateRenderer.cs`
- Create: `src/TrustPanel.Api/Endpoints/EmailEndpoints.cs`
- Create: `src/TrustPanel.Api/Endpoints/ResendWebhookEndpoints.cs`

- [ ] Implement built-in templates: request, follow-up, thank-you, new testimonial notification, weekly digest.
- [ ] Render templates with RazorLight and merge tags for submitter name, form link, workspace name, and custom fields.
- [ ] Use workspace-specific sender name/address where plan allows it.
- [ ] Send email through Resend SDK from Hangfire jobs.
- [ ] Implement Resend webhook endpoint to update `EmailLog` delivery status.
- [ ] Implement signed one-click unsubscribe URL.
- [ ] Store suppressions in `EmailSuppression` and check before every send.
- [ ] Enforce max 3 emails per submitter address per 30 days using Redis TTL counters.
- [ ] Add tests for template rendering, suppression checks, rate cap, webhook status update, and signed unsubscribe.

### Phase 10: Analytics

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/AnalyticsEndpoints.cs`
- Create: `src/TrustPanel.Api/Endpoints/PublicEventEndpoints.cs`
- Create: `src/TrustPanel.Application/Analytics/**`
- Create: `src/TrustPanel.Infrastructure/Jobs/AggregateWidgetAnalyticsJob.cs`

- [ ] Implement `POST /api/public/events` for widget `View` and `Click` events.
- [ ] Use raw SQL insert for event writes to avoid EF overhead.
- [ ] Capture widget ID, testimonial ID, event type, country, device, referrer, and timestamp.
- [ ] Add rate protection suitable for unauthenticated beacon traffic.
- [ ] Add nightly Hangfire aggregation into `WidgetAnalyticsDaily`.
- [ ] Implement dashboard analytics queries for form conversion, submissions over time, rating trend, impressions, CTR, engagement, device, and country.
- [ ] Implement streaming CSV export for analytics data.
- [ ] Add tests for event ingestion, aggregation, dashboard queries, and CSV streaming.

### Phase 11: AI Features

**Files:**
- Create: `src/TrustPanel.Application/Ai/**`
- Create: `src/TrustPanel.Infrastructure/Ai/AnthropicAiService.cs`
- Create: `src/TrustPanel.Infrastructure/Jobs/AnalyzeTestimonialSentimentJob.cs`
- Create: `src/TrustPanel.Infrastructure/Jobs/GenerateWorkspaceInsightsJob.cs`

- [ ] Implement per-testimonial sentiment analysis with `claude-haiku-3-5`.
- [ ] Implement highlight extraction for long text testimonials.
- [ ] Implement thank-you reply suggestion generation.
- [ ] Implement AI-assisted import filtering before saving imported reviews.
- [ ] Implement scheduled insights reports using `claude-sonnet-4-6`.
- [ ] Cache insights report results in Redis for 24 hours.
- [ ] Ensure all AI calls run in Hangfire jobs and never in synchronous request handlers.
- [ ] Add tests using a fake AI service for job behavior, persistence, and cache keys.

### Phase 12: Team and Permissions

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/TeamEndpoints.cs`
- Create: `src/TrustPanel.Application/Teams/**`
- Create: `src/TrustPanel.Api/Security/WorkspaceRoleAuthorizationHandler.cs`
- Create: `src/TrustPanel.Application/Common/Behaviors/AuthorizationBehavior.cs`

- [ ] Implement invitations by email with 7-day signed invitation URL.
- [ ] Implement workspace roles: Owner, Admin, Viewer.
- [ ] Add policy-based authorization with custom `IAuthorizationHandler` checking `WorkspaceMember`.
- [ ] Apply authorization in MediatR pipeline behavior for commands and queries.
- [ ] Append activity log entries on write commands.
- [ ] Add tests for invitation accept, role permissions, viewer write denial, and owner-only billing/delete actions.

### Phase 13: Public REST API and Webhooks

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/PublicApiV1Endpoints.cs`
- Create: `src/TrustPanel.Application/PublicApi/**`
- Create: `src/TrustPanel.Api/Security/ApiKeyAuthenticationHandler.cs`
- Create: `src/TrustPanel.Infrastructure/Integrations/OutboundWebhookDispatcher.cs`

- [ ] Implement API key create, list, rename, revoke.
- [ ] Generate API keys once, store SHA-256 hash, and never return plaintext after creation.
- [ ] Implement API key authentication from `Authorization: Bearer`.
- [ ] Rate limit API keys to 1,000 requests per hour through Redis token bucket.
- [ ] Implement `GET /api/v1/testimonials` for approved testimonial listing with filters.
- [ ] Implement `GET /api/v1/testimonials/{id}`.
- [ ] Implement `POST /api/v1/testimonials` for programmatic creation.
- [ ] Implement outbound webhooks when testimonials are approved.
- [ ] Sign outbound webhook payloads with workspace webhook secret.
- [ ] Expose OpenAPI documentation with Scalar.
- [ ] Add tests for API key hashing, auth, rate limit, public API filters, and webhook delivery.

### Phase 14: Super Admin

**Files:**
- Create: `src/TrustPanel.Api/Endpoints/AdminEndpoints.cs`
- Create: `src/TrustPanel.Application/Admin/**`
- Create: `src/TrustPanel.Api/Security/SuperAdminPolicies.cs`

- [ ] Implement admin listing for users, workspaces, and subscriptions.
- [ ] Implement short-lived impersonation JWT with original admin ID claim.
- [ ] Implement manual plan override for comps and extended trials.
- [ ] Pull MRR from Stripe API rather than storing local MRR totals.
- [ ] Implement active subscriber and churn metrics.
- [ ] Implement email broadcast as Hangfire bulk job filtered by plan or activity segment.
- [ ] Expose Hangfire dashboard at `/hangfire` behind SuperAdmin policy.
- [ ] Add tests for super-admin authorization, impersonation claims, plan override, and broadcast job enqueueing.

### Phase 15: Security Hardening

**Files:**
- Modify: `src/TrustPanel.Api/Program.cs`
- Modify: `src/TrustPanel.Api/Middleware/**`
- Modify: `src/TrustPanel.Api/Security/**`

- [ ] Apply .NET 8 RateLimiter middleware to all API route groups by IP, workspace, form, or API key as appropriate.
- [ ] Ensure every API response follows the required envelope: `code`, `status`, `data`, `message`, `error`, and `errors`.
- [ ] Ensure validation, auth, forbidden, not-found, conflict, rate-limit, webhook, Turnstile, and exception responses use the same envelope.
- [ ] Ensure JWT access tokens expire in 15 minutes.
- [ ] Ensure refresh tokens are stored only in `httpOnly`, `Secure` cookies.
- [ ] Add antiforgery protection for stateful cookie-backed endpoints using double-submit cookie pattern.
- [ ] Validate Stripe signatures before webhook handling.
- [ ] Validate Resend webhook authenticity if provider supports signature verification.
- [ ] Store API keys SHA-256 hashed and only show plaintext once.
- [ ] Validate video MIME type and max size before issuing pre-signed upload URLs.
- [ ] Apply CSP headers on public collection pages.
- [ ] Enforce Redis rate limit of 5 submissions per IP per hour per form.
- [ ] Keep R2 bucket private and issue only pre-signed file URLs.
- [ ] Append immutable audit logs on every write command.
- [ ] Implement GDPR export endpoint streaming submitter data as CSV.
- [ ] Implement GDPR deletion request purging testimonials and personal submitter fields.
- [ ] Confirm no secrets are exposed through frontend-readable config endpoints.
- [ ] Add Sentry release tagging and error grouping for the API.

### Phase 16: Deployment and Operations

**Files:**
- Create: `Dockerfile`
- Create: `docker-compose.prod.yml`
- Create: `nginx/trustpanel.conf`
- Create: `scripts/migrate.ps1`
- Create: `scripts/seed-plans.ps1`
- Create: `docs/backend-ops.md`

- [ ] Containerize the API.
- [ ] Configure Nginx reverse proxy to Kestrel.
- [ ] Configure Certbot or Cloudflare SSL flow.
- [ ] Configure Cloudflare caching for `widget.js` and 60-second TTL for public widget API responses.
- [ ] Configure environment variables for PostgreSQL, Redis, Stripe, Resend, R2, Meilisearch, Anthropic, Turnstile, Sentry, JWT signing, and DataProtection keys.
- [ ] Keep `.env.example` synchronized with deployment docs and every configuration option the backend reads.
- [ ] Add database migration script for deployment.
- [ ] Add recurring Hangfire job registration at startup.
- [ ] Add backup notes for PostgreSQL and R2.
- [ ] Add production readiness checklist covering health checks, logs, alerts, secrets, migrations, webhooks, and rate limits.

## Test Strategy

- Unit tests: domain rules, value objects, validators, MediatR handlers with fake services.
- Integration tests: Minimal API endpoints through WebApplicationFactory, PostgreSQL and Redis via Testcontainers.
- Contract tests: required API response envelope, public widget payload inside `data`, public API v1 payloads, Stripe webhook handling, Resend webhook handling.
- Security tests: tenant isolation, role policies, API key hashing, refresh token revocation, rate limits, signature verification.
- Job tests: Hangfire job classes invoked directly with fake external clients.

Minimum verification before considering backend complete:

```powershell
dotnet format --verify-no-changes
dotnet build
dotnet test
```

## Recommended Build Order

1. Foundation, persistence, auth, workspace, forms, testimonial management, basic widget data, billing.
2. Email, video upload, analytics, AI sentiment, advanced widgets.
3. Agency features: white-label, custom domain, team roles, public API, outbound webhooks.
4. Growth and admin: imports, insights, super admin, broadcasts, GDPR tools, operations hardening.

This order matches the blueprint's revenue-first roadmap: ship the core loop and Stripe by week 4, then add Pro and Agency value.

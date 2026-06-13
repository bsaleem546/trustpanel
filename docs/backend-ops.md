# TrustPanel Backend — Operations Guide

## Table of Contents

1. [First-Time Deployment](#first-time-deployment)
2. [SSL / TLS with Certbot](#ssl--tls-with-certbot)
3. [Cloudflare Caching](#cloudflare-caching)
4. [Database Migrations](#database-migrations)
5. [Backups](#backups)
6. [Recurring Hangfire Jobs](#recurring-hangfire-jobs)
7. [Environment Variables Reference](#environment-variables-reference)
8. [Production Readiness Checklist](#production-readiness-checklist)

---

## First-Time Deployment

```bash
# 1. Clone the repo on the server and copy env vars
cp .env.example .env
# Edit .env with real secrets

# 2. Build and start services
docker compose -f docker-compose.prod.yml up -d --build

# 3. Apply database migrations
./scripts/migrate.ps1
# or on Linux:
dotnet ef database update --project backend/src/TrustPanel.Api

# 4. Seed billing plans
./scripts/seed-plans.ps1

# 5. Tail logs
docker compose -f docker-compose.prod.yml logs -f api
```

---

## SSL / TLS with Certbot

TrustPanel uses Certbot for Let's Encrypt certificates. Nginx serves ACME challenges
on port 80 before the full HTTPS config is active.

```bash
# Initial certificate (Nginx must be running and port 80 reachable)
docker run --rm -v /etc/letsencrypt:/etc/letsencrypt \
  -v /var/www/certbot:/var/www/certbot \
  certbot/certbot certonly --webroot \
  -w /var/www/certbot -d api.trustpanel.io

# Auto-renewal (add to crontab)
0 3 * * * certbot renew --quiet && docker exec trustpanel-nginx-1 nginx -s reload
```

Certbot stores keys under `/etc/letsencrypt/live/api.trustpanel.io/`. The Nginx config
mounts `/etc/letsencrypt` read-only.

---

## Cloudflare Caching

| Path | Cache | Notes |
|------|-------|-------|
| `/api/public/widget/*` | 60 s (`s-maxage=60`) | Set by Nginx; Cloudflare respects `s-maxage` |
| `widget.js` (CDN) | Long-term | Served from R2/CDN, not this API |
| All other API paths | No cache (`no-store`) | Ensures fresh data for dashboard |

Configure Cloudflare Page Rules or Cache Rules if you need longer edge TTLs for widget
payloads under heavy read traffic.

---

## Database Migrations

EF Core migrations are committed as code. Apply them via the script:

```powershell
$env:DATABASE_URL = "Host=..."
.\scripts\migrate.ps1
```

Or directly with the EF Core CLI:

```bash
dotnet ef database update \
  --project backend/src/TrustPanel.Api \
  --startup-project backend/src/TrustPanel.Api
```

Never run `dotnet ef database drop` in production. Use point-in-time recovery from
backups instead.

---

## Backups

### PostgreSQL

```bash
# Manual dump
docker exec trustpanel-postgres-1 pg_dump -U trustpanel trustpanel \
  | gzip > backup-$(date +%Y%m%d-%H%M%S).sql.gz

# Restore
gunzip -c backup-YYYYMMDD-HHMMSS.sql.gz \
  | docker exec -i trustpanel-postgres-1 psql -U trustpanel trustpanel
```

Schedule daily `pg_dump` with `cron` and ship to R2 or S3 for off-site retention.
Keep at least 30 daily snapshots and 4 weekly snapshots.

### Cloudflare R2

R2 objects are versioned at the bucket level. Enable versioning in the Cloudflare
dashboard. For disaster recovery, configure a lifecycle rule to replicate objects to a
secondary bucket in a different region.

---

## Recurring Hangfire Jobs

The following jobs are registered automatically at API startup when Hangfire is enabled
(i.e., `DATABASE_URL` is set and `ASPNETCORE_ENVIRONMENT != Testing`):

| Job ID | Schedule | Purpose |
|--------|----------|---------|
| `verify-workspace-domains` | Hourly | DNS verification for custom domains |
| `aggregate-widget-analytics` | Daily (midnight UTC) | Rolls up `widget_events` into `WidgetAnalyticsDaily` |

One-off jobs (enqueued via `IJobScheduler`):
- `GenerateWorkspaceInsightsJob` — triggered on first AI insights request; caches for 24 h
- `SuggestReplyJob` — triggered on first reply-suggestion request; caches for 24 h
- `AnalyzeTestimonialSentimentJob` — triggered after testimonial submission
- `SendThankYouEmailJob`, `SendOwnerNotificationJob` — post-submission emails

Monitor jobs at `/hangfire` (requires SuperAdmin JWT).

---

## Environment Variables Reference

See `.env.example` for the full list with descriptions. Critical production values:

| Variable | Required | Notes |
|----------|----------|-------|
| `DATABASE_URL` | Yes | PostgreSQL connection string |
| `REDIS_CONNECTION` | Yes | Redis connection string |
| `JWT_SECRET` | Yes | ≥ 64 random characters |
| `JWT_EXPIRY_MINUTES` | Yes | Set to 15 |
| `DATA_PROTECTION_KEYS_PATH` | Yes | Writable directory on persistent volume |
| `STRIPE_SECRET_KEY` | Yes | Live key in production |
| `STRIPE_WEBHOOK_SECRET` | Yes | From Stripe dashboard |
| `RESEND_API_KEY` | Yes | Transactional email |
| `R2_ACCOUNT_ID` | Yes | Required for video upload |
| `ANTHROPIC_API_KEY` | No | AI features degrade gracefully without it |
| `TURNSTILE_SECRET_KEY` | No | Form spam protection; bypassed when unset |
| `SENTRY_DSN` | No | Error tracking |

---

## Production Readiness Checklist

### Health Checks

- [ ] `GET /health` returns 200 with `status: healthy`
- [ ] `GET /health/ready` passes all tagged checks (PostgreSQL, Redis, Hangfire, Meilisearch, R2)
- [ ] Uptime monitor configured (e.g., Better Uptime, Cronitor) hitting `/health`

### Logging & Alerts

- [ ] JSON structured logs shipped to log aggregator (Loki, Datadog, CloudWatch)
- [ ] Sentry DSN set; release tagged via `SENTRY_RELEASE`
- [ ] Alert on error rate spike (> 1 % 5xx over 5 min window)
- [ ] Alert on `/health/ready` failure
- [ ] Alert on Hangfire queue length > 500 jobs

### Secrets

- [ ] `JWT_SECRET` is ≥ 64 random characters, unique per environment
- [ ] `DATA_PROTECTION_KEYS_PATH` points to a persistent, encrypted volume
- [ ] Stripe live keys used only in production
- [ ] No secrets committed to git; `.env` in `.gitignore`
- [ ] R2 bucket is private; URLs are pre-signed with expiry

### Migrations

- [ ] `migrate.ps1` run successfully after every deployment
- [ ] Migration idempotency verified (running twice is safe)
- [ ] Rollback procedure documented and tested

### Webhooks

- [ ] Stripe webhook secret set; signature validation enabled
- [ ] Resend webhook secret set; signature validation enabled
- [ ] Outbound webhook HMAC secrets rotated on first deploy

### Rate Limits

- [ ] Redis-backed `IRateLimiter` reachable in production
- [ ] API key rate limit (1,000 req/h) verified via load test
- [ ] Public form submission rate limit (5/h per IP) verified

### Compliance

- [ ] GDPR export endpoint tested end-to-end
- [ ] GDPR delete endpoint tested; verify personal fields are nulled
- [ ] Cookie consent banner wired to the frontend before launch
- [ ] Privacy policy and terms of service pages live

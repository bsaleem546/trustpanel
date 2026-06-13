# TrustPanel

Multi-tenant testimonial collection SaaS. ASP.NET Core 8 backend + TanStack Start (React 19) frontend.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.x | `dotnet --version` |
| Node.js | 20+ | `node --version` |
| npm | 10+ | ships with Node |
| PostgreSQL | 15+ | local install or Docker |
| Redis | 7+ | optional for dev — app falls back to in-memory |
| Docker | any | optional, for `docker compose` workflow |

---

## Quick start — local development

### 1. Clone and configure

```bash
git clone <repo-url>
cd trustpanel
```

Copy the backend environment file and fill in your local values:

```bash
cp .env.example .env
```

Minimum required for local dev (edit `backend/src/TrustPanel.Api/appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=trustpanel;Username=trustpanel;Password=change-me"
  },
  "REDIS_CONNECTION": "localhost:6379",
  "JWT_SECRET": "dev-secret-at-least-64-chars-long-replace-in-prod-1234567890abcdef"
}
```

> Redis is optional in development. If `REDIS_CONNECTION` is not set, the app uses an in-memory fallback for rate limiting and caching.

---

### 2. Database setup

Start PostgreSQL (or use Docker):

```bash
# Docker one-liner
docker run -d --name trustpanel-pg \
  -e POSTGRES_DB=trustpanel \
  -e POSTGRES_USER=trustpanel \
  -e POSTGRES_PASSWORD=change-me \
  -p 5432:5432 \
  postgres:15-alpine
```

Apply migrations:

```bash
dotnet ef database update \
  --project backend/src/TrustPanel.Api \
  --startup-project backend/src/TrustPanel.Api
```

Seed billing plans:

```powershell
# PowerShell
$env:DATABASE_URL = "Host=localhost;Port=5432;Database=trustpanel;Username=trustpanel;Password=change-me"
.\scripts\seed-plans.ps1
```

---

### 3. Start the backend

```bash
dotnet run --project backend/src/TrustPanel.Api
```

API runs at **http://localhost:5065**  
Swagger UI at **http://localhost:5065/swagger**

---

### 4. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend runs at **http://localhost:3000**

All `/api/*` requests are proxied to `http://localhost:5065` automatically (configured in `vite.config.ts`). No CORS setup needed.

---

## Running both at once (recommended)

Open two terminals:

```bash
# Terminal 1 — backend
dotnet run --project backend/src/TrustPanel.Api

# Terminal 2 — frontend
cd frontend && npm run dev
```

Then open **http://localhost:3000**.

---

## Docker Compose (production-like)

```bash
# Build and start everything (API + Nginx + PostgreSQL + Redis)
docker compose -f docker-compose.prod.yml up -d --build

# Apply migrations
./scripts/migrate.ps1

# Seed plans
./scripts/seed-plans.ps1

# View logs
docker compose -f docker-compose.prod.yml logs -f api
```

---

## Backend commands

```bash
# Build
dotnet build backend/TrustPanel.sln

# Run (default port 5065)
dotnet run --project backend/src/TrustPanel.Api

# Run on a specific port
dotnet run --project backend/src/TrustPanel.Api --urls "http://localhost:5065"

# Watch mode (auto-restart on file save)
dotnet watch --project backend/src/TrustPanel.Api

# Run all tests
dotnet test backend/TrustPanel.sln

# Run a specific test class
dotnet test backend/TrustPanel.sln --filter "FullyQualifiedName~AuthTests"

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> \
  --project backend/src/TrustPanel.Infrastructure \
  --startup-project backend/src/TrustPanel.Api

# Apply migrations
dotnet ef database update \
  --project backend/src/TrustPanel.Api \
  --startup-project backend/src/TrustPanel.Api

# Check formatting
dotnet format backend/TrustPanel.sln --verify-no-changes
```

---

## Frontend commands

```bash
cd frontend

# Install dependencies
npm install

# Start dev server (http://localhost:3000)
npm run dev

# Production build
npm run build

# Preview production build locally
npm run preview

# Type-check without emitting
npx tsc --noEmit
```

---

## Project structure

```
trustpanel/
├── backend/
│   ├── src/
│   │   ├── TrustPanel.Domain/          # Entities, enums, value objects
│   │   ├── TrustPanel.Application/     # MediatR commands/queries, interfaces
│   │   ├── TrustPanel.Infrastructure/  # EF Core, Redis, Stripe, Resend, R2, AI
│   │   └── TrustPanel.Api/             # ASP.NET Core Minimal API, endpoints
│   └── tests/
│       └── TrustPanel.IntegrationTests/ # Testcontainers + WebApplicationFactory
├── frontend/
│   └── src/
│       ├── lib/api/    # Typed API clients (one file per domain)
│       ├── routes/     # TanStack Router file-based pages
│       └── components/ # UI components (shadcn/ui + custom)
├── nginx/              # Nginx reverse proxy config
├── scripts/            # migrate.ps1, seed-plans.ps1
├── docs/               # backend-ops.md
├── docker-compose.prod.yml
├── .env.example
└── README.md
```

---

## Environment variables

See [`.env.example`](.env.example) for the full list. Key ones for local dev:

| Variable | Default / notes |
|----------|----------------|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `REDIS_CONNECTION` | Optional — in-memory fallback used when absent |
| `JWT_SECRET` | Must be ≥ 64 characters |
| `JWT_EXPIRY_MINUTES` | `15` |
| `ANTHROPIC_API_KEY` | Optional — AI features disabled when absent |
| `STRIPE_SECRET_KEY` | Optional — billing disabled when absent |
| `RESEND_API_KEY` | Optional — emails logged to console when absent |
| `TURNSTILE_SECRET_KEY` | Optional — form submissions pass through when absent |

For the frontend, create `frontend/.env.local`:

```env
# Only needed if the backend runs on a non-default port
VITE_API_BASE_URL=
```

Leave `VITE_API_BASE_URL` empty in development — the Vite proxy handles it.

---

## Ops & deployment

See [`docs/backend-ops.md`](docs/backend-ops.md) for:
- SSL/TLS with Certbot
- Cloudflare caching configuration
- PostgreSQL and R2 backup procedures
- Recurring Hangfire job registration
- Production readiness checklist

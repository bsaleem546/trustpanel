# TrustPanel

Multi-tenant testimonial collection SaaS. ASP.NET Core 8 backend + TanStack Start (React 19) frontend.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Docker | any | runs the backend, database, redis, search |
| Node.js | 20+ | `node --version` |
| npm | 10+ | ships with Node |
| .NET SDK | 8.x | only needed to run EF migrations or tests |

---

## Quick start

### 1. Clone and configure

```bash
git clone <repo-url>
cd trustpanel
```

Copy and fill in the two env files (one per service):

```bash
cp backend/.env.example backend/.env
cp frontend/.env.example frontend/.env
```

Edit `backend/.env` with your values — both files are gitignored so secrets are never committed.

---

### 2. Start the backend (Docker)

```bash
docker compose up -d --build
```

This starts the .NET API, PostgreSQL, Redis, and Meilisearch. The API is available at **http://localhost:5065**.

Run migrations and seed plans (first time only):

```bash
./scripts/migrate.ps1
./scripts/seed-plans.ps1
```

---

### 3. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:3000** — Vite proxies all `/api/*` requests to the backend automatically.

---

## Docker commands

```bash
# Build and start the backend (API + PostgreSQL + Redis + Meilisearch)
docker compose up -d --build

# Apply migrations
./scripts/migrate.ps1

# Seed billing plans
./scripts/seed-plans.ps1

# View API logs
docker compose logs -f api

# Stop everything
docker compose down
```

Then start the frontend separately:

```bash
cd frontend && npm run dev
```

Open **http://localhost:3000** — Vite proxies all `/api/*` requests to the backend at port 5065.

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
└── README.md
```

---

## Environment variables

See [`backend/.env.example`](backend/.env.example) for the full list. Key ones for local dev:

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

For the frontend, `frontend/.env` is already copied from `frontend/.env.example` in step 1.

Leave `VITE_API_BASE_URL` empty in development — the Vite proxy handles it. Set it to your backend's public URL in production (e.g. `https://api.trustpanel.io`).

---

## Ops & deployment

See [`docs/backend-ops.md`](docs/backend-ops.md) for:
- SSL/TLS with Certbot
- Cloudflare caching configuration
- PostgreSQL and R2 backup procedures
- Recurring Hangfire job registration
- Production readiness checklist

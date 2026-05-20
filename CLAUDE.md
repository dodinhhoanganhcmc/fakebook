# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Fakebook — a Facebook clone built on **.NET Aspire**. Stack: ASP.NET (minimal API) backend, Redis cache, React 19 + Vite + TypeScript frontend, PostgreSQL database (intended, see "Not yet wired"). Targets **.NET 10**; Aspire SDK/packages are **13.1.0**.

The solution lives in the `Fakebook/` subdirectory (not the repo root). Run commands from there.

## Commands

All backend commands assume you are in `Fakebook/`.

```bash
# Run the WHOLE app (AppHost orchestrates Redis + server + frontend, opens the Aspire dashboard)
dotnet run --project Fakebook.AppHost

# Build the solution
dotnet build Fakebook.slnx
```

The Aspire dashboard is the control plane: it launches all resources, shows logs/traces/metrics, and proxies endpoints. Default dashboard/app URLs are in `Fakebook.AppHost/Properties/launchSettings.json` (https profile: app at `https://localhost:17198`).

Frontend (Aspire normally starts this for you; run standalone only when iterating on UI alone — from `Fakebook/frontend/`):

```bash
npm install
npm run dev      # vite dev server
npm run build    # tsc -b && vite build
npm run lint     # eslint
```

**Tests:** none exist yet. There is no test project in the solution.

## Architecture — the big picture

**AppHost is the entry point, not the server.** `Fakebook.AppHost/AppHost.cs` is the orchestration root. It declares resources and their dependencies; running it brings up everything. Never start `Fakebook.Server` directly for normal dev — start AppHost so service discovery, Redis, and the frontend are wired. Current wiring:
- `cache` = Redis (`AddRedis`)
- `server` = the API project, references `cache`, waits for it, exposes `/health`, has external HTTP endpoints
- `webfrontend` = the Vite app (`AddViteApp`), references `server`, waits for it
- On publish: `server.PublishWithContainerFiles(webfrontend, "wwwroot")` copies the built frontend into the server's `wwwroot`.

**Dev vs. prod frontend serving differ.** In dev, the frontend is a separate Vite process; `frontend/vite.config.ts` proxies `/api/*` to the server using the `SERVER_HTTPS` / `SERVER_HTTP` env vars that Aspire injects. In prod, the frontend is built into `server/wwwroot` and served by `app.UseFileServer()` (see `Fakebook.Server/Program.cs`). So API calls are always same-origin `/api/...` from the frontend's perspective — keep new endpoints under the `/api` group.

**Service defaults live inside the server project, not a separate project.** `Fakebook.Server/Extensions.cs` declares `AddServiceDefaults()` / `MapDefaultEndpoints()` under namespace `Microsoft.Extensions.Hosting` (unusual placement — normally a `ServiceDefaults` project). It centrally configures OpenTelemetry (logging/metrics/tracing), service discovery, HTTP resilience handlers, and health checks (`/health`, `/alive` — dev-only by default). Cross-cutting host config belongs here.

**Redis is wired as OutputCache, not a general data cache.** `Program.cs` uses `AddRedisClientBuilder("cache").WithOutputCache()` + `app.UseOutputCache()`, and endpoints opt in via `.CacheOutput(...)`. If you need Redis as an app-level distributed cache or for sessions/pub-sub, that's a separate integration to add.

**API style:** minimal API. Endpoints are mapped on a `MapGroup("/api")` in `Program.cs`. The only current endpoint is the template `weatherforecast` sample — replace it as real features land.

## Current state / not yet wired

This is essentially the stock Aspire React starter with no domain code yet. When building features, expect to add these first:
- **PostgreSQL is part of the intended stack but is NOT in `AppHost.cs`.** Add it with `Aspire.Hosting.PostgreSQL` in AppHost, reference it from `server`, and add an EF Core / Npgsql client integration in the server. No DbContext, entities, or migrations exist.
- **No auth/identity, no domain models** (User, Post, Comment, Friendship, etc.).
- **Frontend is the template UI** (weather demo in `src/App.tsx`). No router, API client layer, or state management yet.

## Conventions

- C# projects: `Nullable` and `ImplicitUsings` enabled. Keep them on.
- Frontend API calls go to relative `/api/...` (never hardcode the server host — the Vite proxy / file server handle origin).
- Add new backend service dependencies (DBs, queues, etc.) in `AppHost.cs` as Aspire resources, then consume them via the matching `Aspire.*` client integration in the server — don't hand-roll connection strings.

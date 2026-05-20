# Fakebook

A Facebook-style social network, built as a learning/portfolio recreation of Meta's Facebook using a modern **.NET Aspire** stack. Fakebook is a distributed application: an ASP.NET API backend, a React single-page frontend, Redis for caching, and PostgreSQL for persistent data — all orchestrated and observed through the Aspire AppHost and dashboard.

> **Status:** early scaffolding. The solution currently runs the stock Aspire + React starter (a weather-forecast demo). The social-network domain (users, posts, friendships, feed, etc.) is not implemented yet. See [Roadmap](#roadmap).

---

## Tech stack

| Layer            | Technology                                                        |
| ---------------- | ----------------------------------------------------------------- |
| Orchestration    | .NET Aspire 13.1.0 (AppHost + dashboard)                          |
| Backend API      | ASP.NET (minimal APIs) on .NET 10                                 |
| Frontend         | React 19, TypeScript, Vite 7                                      |
| Cache            | Redis (wired as ASP.NET Output Cache)                             |
| Database         | PostgreSQL *(intended — not yet wired into the AppHost)*          |
| Observability    | OpenTelemetry (logs, metrics, traces) via Aspire service defaults |

Why Aspire? It gives a single command to spin up the whole system locally — backend, frontend, Redis, and (later) Postgres — with built-in service discovery, health checks, resilience, and a dashboard for logs/traces/metrics. No manual `docker-compose` wiring or connection-string juggling.

---

## Architecture

```
                ┌─────────────────────────────────────────┐
                │           Fakebook.AppHost                │
                │   (Aspire orchestration + dashboard)      │
                └───────────────┬───────────────┬──────────┘
                                │ orchestrates  │
              ┌─────────────────┘               └────────────────┐
              ▼                                                   ▼
   ┌────────────────────┐                            ┌────────────────────────┐
   │   Fakebook.Server   │◀──── /api proxy ─────────│        frontend          │
   │  (ASP.NET minimal   │                            │  (React + Vite + TS)    │
   │   API, .NET 10)     │                            └────────────────────────┘
   └─────┬─────────┬─────┘
         │         │
         ▼         ▼
   ┌──────────┐ ┌──────────────────┐
   │  Redis   │ │   PostgreSQL      │
   │ (cache)  │ │  (planned)        │
   └──────────┘ └──────────────────┘
```

### Key design points

- **AppHost is the entry point.** You run `Fakebook.AppHost`, not the server directly. It declares each resource (Redis, the server, the frontend) and their dependencies, then starts them together. This is what provides service discovery and the dashboard.
- **The frontend talks to the API at a relative `/api/...` path.**
  - *In development:* the React app runs as a separate Vite dev server; `vite.config.ts` proxies `/api/*` to the backend using host info Aspire injects (`SERVER_HTTPS` / `SERVER_HTTP`).
  - *In production:* the built frontend is published into the server's `wwwroot` and served as static files, so API calls are same-origin.
- **Shared host configuration lives in `Fakebook.Server/Extensions.cs`** (`AddServiceDefaults` / `MapDefaultEndpoints`): OpenTelemetry, service discovery, HTTP resilience, and health checks (`/health`, `/alive`).
- **Redis is currently used as an HTTP Output Cache** (endpoints opt in with `.CacheOutput(...)`), not yet as a general-purpose distributed cache.

---

## Project structure

```
Fakebook/                       # repo root
├─ CLAUDE.md                    # guidance for Claude Code
├─ README.md                    # this file
└─ Fakebook/                    # solution root (Fakebook.slnx)
   ├─ Fakebook.AppHost/         # Aspire orchestration — START HERE
   │  └─ AppHost.cs             # declares Redis, server, frontend resources
   ├─ Fakebook.Server/          # ASP.NET minimal-API backend
   │  ├─ Program.cs             # endpoints, Redis output cache, file server
   │  └─ Extensions.cs          # Aspire service defaults (telemetry, health, resilience)
   └─ frontend/                 # React + Vite + TypeScript SPA
      ├─ src/                    # App.tsx, main.tsx, styles
      └─ vite.config.ts          # dev proxy of /api -> server
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [.NET Aspire workload / tooling](https://learn.microsoft.com/dotnet/aspire/) (13.x)
- [Node.js](https://nodejs.org/) (LTS) + npm — for the frontend
- A container runtime (e.g. **Docker Desktop** or **Podman**) — Aspire runs Redis (and later Postgres) as containers

---

## Getting started

From the solution directory (`Fakebook/`):

```bash
# Restore and run the entire application via Aspire.
# This starts Redis, the API server, the React frontend, and the Aspire dashboard.
dotnet run --project Fakebook.AppHost
```

The Aspire dashboard opens automatically. From there you can reach the frontend and API, and inspect logs, traces, and metrics for every resource. Default URLs are defined in `Fakebook.AppHost/Properties/launchSettings.json`.

### Working on the frontend alone

Aspire normally launches the frontend for you. To iterate on the UI in isolation, from `Fakebook/frontend/`:

```bash
npm install
npm run dev      # Vite dev server
npm run build    # type-check + production build (tsc -b && vite build)
npm run lint     # ESLint
```

> Running the frontend standalone means `/api` calls have no backend to proxy to unless the server is also running.

---

## Build

```bash
# From Fakebook/
dotnet build Fakebook.slnx
```

## Tests

No test project exists yet. Testing setup is part of the roadmap.

---

## Roadmap

The current code is the starter template. Planned work, roughly in order:

1. **Wire PostgreSQL** into `AppHost.cs` (`Aspire.Hosting.PostgreSQL`) and add EF Core / Npgsql to the server.
2. **Domain model & schema** — `User`, `Post`, `Comment`, `Like`, `Friendship`, `Notification`, etc., with EF Core migrations.
3. **Authentication & identity** — registration, login, sessions/JWT.
4. **Core social features** — profiles, news feed, posting, comments, reactions, friend requests.
5. **Frontend application shell** — routing, API client layer, auth flows, state management (replacing the demo UI).
6. **Real-time** — notifications / messaging (e.g. SignalR), using Redis for pub/sub.
7. **Testing** — backend and frontend test suites.

---

## License

Not yet specified. This is an educational recreation and is not affiliated with Meta or Facebook.

# Fakebook — run guide

Facebook clone on **.NET 10 + Aspire 13.1 + PostgreSQL + Redis + React 19 (Vite)**.
JWT auth, EF Core migrations, seeded demo data.

---

## 1. Prerequisites (all platforms)

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.100+** | `dotnet --version` |
| Node.js | 20+ | `node -v` |
| PostgreSQL | 14+ (16/17/18 tested) | local instance on port 5432 |
| Docker (optional) | latest | only if you want the Aspire-managed Redis container |
| `dotnet-ef` tool | 10.0.x | `dotnet tool install --global dotnet-ef --version 10.0.0` |

> The repo is configured to run against a **local Postgres** (not a container) so the Aspire AppHost stays lightweight. Redis is launched by Aspire as a container when you run via `Fakebook.AppHost`.

### Install hints

- **Windows** — install .NET 10 SDK from <https://dotnet.microsoft.com/download>, PostgreSQL from <https://www.postgresql.org/download/windows/> (or `winget install PostgreSQL.PostgreSQL.17`), Node from <https://nodejs.org/>.
- **macOS** — `brew install --cask dotnet-sdk` + `brew install postgresql@17 node`. Start Postgres: `brew services start postgresql@17`.
- **Linux (Ubuntu/Debian)** — Microsoft .NET repo for SDK; `sudo apt install postgresql nodejs npm`; `sudo systemctl start postgresql`.

---

## 2. Configure environment

The app reads `.env` at `Fakebook/.env`. Copy the template:

```bash
# from repo root
cp Fakebook/.env.example Fakebook/.env
```

Edit `Fakebook/.env`:

```
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_USER=app_backend
POSTGRES_PASSWORD=admin
POSTGRES_DB=fakebook_db

ConnectionStrings__fakebookdb=Host=localhost;Port=5432;Database=fakebook_db;Username=app_backend;Password=admin;Include Error Detail=true

JWT_ISSUER=fakebook
JWT_AUDIENCE=fakebook-clients
JWT_SECRET=replace-with-32+-char-random-secret
JWT_ACCESS_TOKEN_MINUTES=60
JWT_REFRESH_TOKEN_DAYS=14

ASPNETCORE_ENVIRONMENT=Development
```

`.env` is git-ignored. Replace `JWT_SECRET` with at least 32 random characters in any environment you care about.

---

## 3. Create the Postgres role + database

Open a `psql` shell as a superuser (`postgres`):

```bash
# Linux/macOS
sudo -u postgres psql

# Windows (default install path)
"C:\Program Files\PostgreSQL\17\bin\psql.exe" -U postgres
# or wherever your install lives — this repo's dev box uses
"E:\PostgreSQL\18\bin\psql.exe" -U postgres
```

Inside `psql`:

```sql
CREATE ROLE app_backend WITH LOGIN PASSWORD 'admin';
CREATE DATABASE fakebook_db OWNER app_backend;
GRANT ALL PRIVILEGES ON DATABASE fakebook_db TO app_backend;
\q
```

(Adjust the role/password/db name to match your `.env`.)

Tables are created automatically on first boot via `db.Database.MigrateAsync()`, so you do **not** need to run `dotnet ef database update` manually for first-time setup.

---

## 4. Run the whole app (Aspire)

This is the normal dev workflow. The AppHost orchestrates Redis, the API server, and the Vite frontend, and opens the Aspire dashboard.

```bash
cd Fakebook
dotnet run --project Fakebook.AppHost
```

You should see:

```
Login to the dashboard at https://localhost:17198/login?t=...
```

Open that URL. From the dashboard you can:
- click the **server** resource → its app URL (e.g. `https://localhost:7xxx`) — that's the API
- click **webfrontend** → opens the Vite dev server (typically `https://localhost:5173`)

### What Aspire starts

| Resource | What it is | Source |
|---|---|---|
| `cache` | Redis container | `AddRedis("cache")` |
| `fakebookdb` | connection string to your local Postgres | `AddConnectionString(...)` |
| `server` | the .NET 10 API | `Fakebook.Server` |
| `webfrontend` | Vite dev server | `Fakebook/frontend` |

Frontend → backend calls use relative `/api/...`. In dev, `Fakebook/frontend/vite.config.ts` proxies them to the server URL Aspire injects.

---

## 5. Run backend + frontend without Aspire (optional)

Useful if Docker isn't available (Aspire needs it for Redis) or you want to iterate on one piece alone.

### Backend only

The server falls back to in-memory cache when Redis isn't reachable only if you remove the Redis client integration. The supported path is to **install Redis locally** and point the server at it:

```bash
# Linux/macOS
brew install redis && brew services start redis
# Windows: use Memurai or Redis on WSL

cd Fakebook/Fakebook.Server
dotnet run
```

The server reads `Fakebook/.env`, applies migrations, seeds demo data, then listens on the URL from `Properties/launchSettings.json` (default profile uses `http://localhost:5xxx`). Override the port:

```bash
ASPNETCORE_URLS="http://localhost:5111" dotnet run --no-launch-profile
```

> **Windows note:** if `dotnet run` complains the address is in use, kill the previous PID:
> `netstat -ano | findstr :5111` then `taskkill /F /PID <pid>`.

### Frontend only

```bash
cd Fakebook/frontend
npm install
npm run dev
```

By default the proxy expects the server URL via `SERVER_HTTPS` / `SERVER_HTTP` env vars (injected by Aspire). When running standalone, point Vite at your backend manually:

```bash
# macOS/Linux
SERVER_HTTP=http://localhost:5111 npm run dev

# Windows PowerShell
$env:SERVER_HTTP="http://localhost:5111"; npm run dev

# Windows cmd
set SERVER_HTTP=http://localhost:5111 && npm run dev
```

---

## 6. Seeded demo accounts

`Seeder.cs` populates four users on first boot if the DB is empty. Password for all: `Password123!`

| Username | Email | Friends |
|---|---|---|
| `alice` | `alice@fakebook.local` | bob, carol |
| `bob`   | `bob@fakebook.local`   | alice, carol |
| `carol` | `carol@fakebook.local` | alice, bob |
| `dave`  | `dave@fakebook.local`  | sent pending request to alice |

Plus three seeded posts, a few comments and reactions.

---

## 7. API surface

All endpoints are under `/api`. Auth uses `Authorization: Bearer <accessToken>`.

### Auth (`/api/auth`)
- `POST /register` — body `{ username, email, password, displayName }`
- `POST /login`    — body `{ usernameOrEmail, password }`
- `POST /refresh`  — body `{ refreshToken }`
- `POST /logout`   — body `{ refreshToken }`

### Users (`/api/users`) — auth required
- `GET  /me`
- `PUT  /me`               — `{ displayName?, bio?, birthDate?, gender?, location? }`
- `PUT  /me/avatar`        — `{ avatarUrl }`
- `GET  /me/activities?take=50`
- `GET  /{userId}`
- `GET  /search?q=&take=20`

### Friends (`/api/friends`) — auth required
- `GET  /`
- `GET  /requests/incoming`
- `GET  /requests/outgoing`
- `POST /requests`                     — `{ targetUserId }`
- `POST /requests/{friendshipId}/accept`
- `POST /requests/{friendshipId}/decline`
- `DELETE /{friendshipId}`

### Posts (`/api/posts`) — auth required
- `POST /`                             — `{ content, imageUrl?, privacy: 0|1|2 }`
- `GET  /{postId}`
- `GET  /user/{userId}?skip=&take=`
- `PUT  /{postId}`                     — same body as POST
- `DELETE /{postId}`                   — soft delete
- `POST /{postId}/share`               — `{ message? }`
- `GET  /{postId}/comments`
- `POST /{postId}/comments`            — `{ content, parentCommentId? }`
- `PUT  /{postId}/comments/{commentId}`
- `DELETE /{postId}/comments/{commentId}`
- `POST /{postId}/reactions`           — `{ type: 0..5 }` *(Like, Love, Haha, Wow, Sad, Angry)*
- `DELETE /{postId}/reactions`
- `POST /{postId}/comments/{commentId}/reactions` — same `{ type }`

### Feed (`/api/feed`) — auth required
- `GET / ?skip=&take=` — posts from you + friends, honoring privacy

### Privacy enum
`0` Public · `1` FriendsOnly · `2` Private

### Quick curl smoke test

```bash
# log in
TOKEN=$(curl -s -X POST http://localhost:5111/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usernameOrEmail":"alice","password":"Password123!"}' \
  | python -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")

# get feed
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5111/api/feed/

# create post
curl -s -X POST http://localhost:5111/api/posts/ \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"content":"hello from curl","imageUrl":null,"privacy":0}'
```

OpenAPI: `GET /openapi/v1.json` in Development.

---

## 8. Manual EF Core commands

You only need these if you change entities and want to add a new migration.

```bash
cd Fakebook/Fakebook.Server

# create a new migration
dotnet ef migrations add <Name> --output-dir Data/Migrations

# apply migrations against the .env database
dotnet ef database update

# roll back to a specific migration
dotnet ef database update <PreviousMigrationName>
```

The `FakebookDbContextFactory` in `Data/DesignTimeFactory.cs` lets `dotnet ef` work without booting the AppHost.

---

## 9. Troubleshooting

| Symptom | Fix |
|---|---|
| `password authentication failed for user "app_backend"` | Postgres role/password don't match `.env`. Recreate role or update `.env`. |
| `Failed to bind to address ... address already in use` | Kill the stale process: `netstat -ano \| findstr :<port>` then `taskkill /F /PID <pid>` (Win) or `lsof -i :<port>` + `kill -9 <pid>` (mac/Linux). |
| Build error `Could not copy ... Npgsql.dll ... locked by Fakebook.Server` | Server still running. Stop it first. |
| `There is not enough space on the disk` during `dotnet add package` | NuGet cache is on a full drive. Move it: set `NUGET_PACKAGES` to a folder on a drive with space, e.g. `export NUGET_PACKAGES=/path/with/space`. Repo already ships a `nuget.config` pointing at `E:\nuget-packages` on Windows — change it on other OSes. |
| Aspire dashboard cannot start Redis | Docker isn't running. Start Docker Desktop / `systemctl start docker`, or run the server standalone with a local Redis. |
| `dotnet ef` "tools version older than runtime" warning | Harmless. Update with `dotnet tool update --global dotnet-ef --version 10.0.1`. |

---

## 10. Project layout

```
Fakebook/
  .env                          ← local config (git-ignored)
  .env.example
  Fakebook.AppHost/             ← Aspire orchestrator
    AppHost.cs                  (wires postgres connection-string, redis, server, frontend)
  Fakebook.Server/              ← ASP.NET 10 minimal API
    Auth/                       (JwtOptions, TokenService, CurrentUser)
    Data/                       (FakebookDbContext, Migrations, Seeder, DesignTimeFactory)
    Domain/                     (Entities + Enums)
    Dtos/
    Endpoints/                  (AuthEndpoints, UserEndpoints, FriendEndpoints, PostEndpoints, FeedEndpoints)
    Program.cs                  (composition root: EF + JWT + endpoint groups)
    Extensions.cs               (Aspire service defaults)
  frontend/                     ← React 19 + Vite + TS
```

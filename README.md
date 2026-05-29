# Multi-Tenant Task Management

Take-home assignment: ASP.NET Core API, React SPA, and WPF desktop companion with JWT authentication, role-based authorization, and tenant-isolated data.

## How to run

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

### API

```bash
cd "f:\Task Mamagement"
dotnet restore
dotnet run --project src/TaskManagement.Api
```

API: `http://localhost:5000`  
Swagger (Development): `http://localhost:5000/swagger`

The database is SQLite (`taskmanagement.db` in the API project folder). Migrations run automatically on startup with seed data.

### Web client

```bash
cd client
npm install
npm run dev
```

Open `http://localhost:5173`. The Vite dev server proxies `/api` to the API.

### Desktop (WPF)

Start the API first, then:

```bash
dotnet run --project desktop/TaskManagement.Desktop
```

### Tests

```bash
dotnet test
```

## Demo accounts

| Tenant    | Email            | Password      | Role  |
|-----------|------------------|---------------|-------|
| Acme Corp | admin@acme.com   | Password123!  | Admin |
| Acme Corp | user@acme.com    | Password123!  | User  |
| Globex Inc| admin@globex.com | Password123!  | Admin |

Admins can delete any task in their tenant. Users can create and complete tasks, and edit only tasks they created.

## Architecture overview

```
TaskManagement.Api          → HTTP, middleware, controllers
TaskManagement.Application  → Services, DTOs, validators, interfaces
TaskManagement.Domain       → Entities, repository contracts
TaskManagement.Infrastructure → EF Core, JWT, repositories, seeding
client/                     → React SPA
desktop/                    → WPF companion
tests/                      → xUnit + Moq + FluentAssertions
```

**Multi-tenancy:** Every task and user belongs to a `TenantId`. Repositories always filter by the tenant from the JWT `tenant_id` claim. Cross-tenant access is not possible through the API.

**Authentication:** JWT bearer tokens issued at login. Claims include user id (`sub`), role, and tenant id/name.

## Key decisions and trade-offs

- **SQLite** instead of SQL Server for zero-install local development. The assignment allows this; production would use SQL Server with the same EF model.
- **Optimized raw SQL** (`GetTaskSummariesByTenantAsync`) instead of a stored procedure because SQLite does not support stored procedures. On SQL Server this query would map cleanly to a stored procedure.
- **BCrypt** for password hashing via `BCrypt.Net-Next` — simple, battle-tested, no ASP.NET Identity overhead for a focused demo.
- **FluentValidation** for request validation, keeping controllers thin.
- **Global exception middleware** for consistent API error responses and Serilog for structured logging.
- **WPF code-behind** rather than full MVVM to keep the desktop scope small while still demonstrating API integration.

## What I would improve with more time

- Integration tests with `WebApplicationFactory` and tenant isolation tests
- Refresh tokens and password reset flows
- SQL Server + real stored procedure deployment
- Full MVVM on desktop with user settings for API URL
- Azure App Service deployment wired to Key Vault for JWT secrets
- Pagination, filtering, and audit log for task changes
- OpenAPI-generated TypeScript client for the SPA

## Libraries used and why

| Library | Purpose |
|---------|---------|
| **Entity Framework Core** | ORM, migrations, SQLite provider |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | JWT validation on API requests |
| **BCrypt.Net-Next** | Secure password hashing |
| **FluentValidation** | Declarative request validation |
| **Serilog.AspNetCore** | Structured logging to console |
| **Swashbuckle.AspNetCore** | OpenAPI/Swagger in development |
| **Moq** | Mocking dependencies in unit tests |
| **FluentAssertions** | Readable test assertions |
| **React + Vite + react-router-dom** | Fast SPA toolchain and client routing |

## CI/CD (bonus)

See `azure-pipelines.yml` for a sample pipeline: restore, build, test, and publish the API artifact.

## Known limitations

- JWT secret is in `appsettings.json` for local dev only
- No refresh tokens; clients must re-login after expiry
- Desktop app uses a hard-coded API URL (`http://localhost:5000`)
- Email addresses are case-sensitive in seed data (login normalizes via repository lookup on lowercase)

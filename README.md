# Multi-Tenant Task Management System

A full-stack task management application demonstrating ASP.NET Core Web API, React SPA, and WPF desktop client with multi-tenant data isolation, JWT authentication, and role-based authorization.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Multi-Tenancy Strategy](#multi-tenancy-strategy)
4. [Authentication & Authorization](#authentication--authorization)
5. [Database Design](#database-design)
6. [API Endpoints](#api-endpoints)
7. [Running the Application](#running-the-application)
8. [Testing](#testing)
9. [Technology Choices & Trade-offs](#technology-choices--trade-offs)
10. [Future Improvements](#future-improvements)

---

## Architecture Overview

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   Web API   │  │  React SPA  │  │      WPF Desktop        │  │
│  │ Controllers │  │   (Vite)    │  │    (MVVM Toolkit)       │  │
│  └──────┬──────┘  └──────┬──────┘  └───────────┬─────────────┘  │
└─────────┼────────────────┼─────────────────────┼────────────────┘
          │                │                     │
          │                │                     │
          │                └────────────────┬────┘
          ▼                                 │
┌───────────────────────────────────────────┼─────────────────────┐
│                    Application Layer      ▼                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Services   │  │    DTOs     │  │ Validators  │              │
│  │ (Business   │  │ (Data       │  │ (FluentVal) │              │
│  │  Logic)     │  │  Transfer)  │  └─────────────┘              │
│  └──────┬──────┘  └─────────────┘                               │
└─────────┼───────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Entities   │  │ Interfaces  │  │    Enums    │              │
│  │ (TaskItem,  │  │ (ITaskRepo, │  │ (UserRole)  │              │
│  │  User, etc) │  │  IUserRepo) │  └─────────────┘              │
│  └─────────────┘  └──────┬──────┘                               │
└──────────────────────────┼──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │Repositories │  │ DbContext   │  │  Services   │              │
│  │ (EF Core    │  │ (SQLite/    │  │ (JWT, Hash, │              │
│  │  Impl)      │  │  SQL Server)│  │  CurrentUser│              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

**Why this architecture?**

I chose Clean Architecture because it enforces dependency inversion—the domain and application layers have no knowledge of infrastructure concerns. This means:

- Business logic in `TaskService` can be unit tested without a database
- Swapping SQLite for SQL Server requires zero changes to services or controllers
- Each layer has a single responsibility, making the codebase maintainable

---

## Project Structure

```
src/
├── TaskManagement.Api/                 # ASP.NET Core Web API
│   ├── Controllers/                    # HTTP endpoints (thin controllers)
│   ├── Middleware/                     # Exception handling, logging
│   └── Program.cs                      # DI configuration, pipeline
│
├── TaskManagement.Application/         # Business logic layer
│   ├── Services/                       # TaskService, AuthService, DashboardService
│   ├── DTOs/                           # Request/Response objects
│   ├── Validators/                     # FluentValidation rules
│   ├── Interfaces/                     # ICurrentUserService, ITokenService
│   ├── Exceptions/                     # NotFoundException, ForbiddenException
│   └── Mapping/                        # AutoMapper profiles
│
├── TaskManagement.Domain/              # Core domain (no dependencies)
│   ├── Entities/                       # TaskItem, User, Tenant
│   ├── Interfaces/                     # ITaskRepository, IUserRepository
│   └── Enums/                          # UserRole constants
│
└── TaskManagement.Infrastructure/      # External concerns
    ├── Persistence/                    # DbContext, configurations, migrations
    ├── Repositories/                   # Repository implementations
    └── Services/                       # JwtTokenService, BcryptPasswordHasher

client/                                 # React + TypeScript + Vite SPA
desktop/TaskManagement.Desktop/         # WPF + MVVM Toolkit
tests/TaskManagement.Tests/             # xUnit unit tests
```

**Why this structure?**

Each project has a single responsibility. The API project only handles HTTP concerns. The Application project contains all business rules. The Domain project defines what a "Task" is without caring how it's stored. The Infrastructure project implements the "how"—database access, token generation, etc.

---

## Multi-Tenancy Strategy

### Approach: Shared Database, Shared Schema

Every tenant-scoped table includes a `TenantId` column. All data access is filtered by the authenticated user's tenant.

```csharp
// Repository always filters by tenant
public async Task<IReadOnlyList<TaskItem>> GetByTenantAsync(Guid tenantId, ...)
{
    return await _context.Tasks
        .Where(t => t.TenantId == tenantId)  // Tenant isolation
        .ToListAsync();
}
```

### How tenant isolation is enforced

1. User logs in with `username` and `password`
2. JWT is issued containing `sub` (userId), `tenantId`, and `role` claims
3. Every request, `ICurrentUserService` extracts `TenantId` from the JWT
4. Repositories **always** filter queries by `TenantId`
5. **Result: Tenant A can never see Tenant B's data**

### Why I chose this approach

| Approach                   | Pros                                    | Cons                                           |
|----------------------------|-----------------------------------------|------------------------------------------------|
| **Shared schema** (chosen) | Simple, fast queries, easy maintenance  | Row-level security relies on application logic |
| Schema-per-tenant          | Stronger isolation                      | Complex migrations, connection management      |
| Database-per-tenant        | Complete isolation, compliance-friendly | Expensive, complex deployment                  |

For this assessment, shared-schema multi-tenancy demonstrates the core requirement (tenant isolation) while keeping implementation complexity appropriate. In a production SaaS with regulatory requirements (HIPAA, SOC2), I would evaluate schema-per-tenant with row-level security policies at the database level.

---

## Authentication & Authorization

### JWT Authentication

```
POST /api/auth/login
{
  "username": "admin@acme.com",
  "password": "Password123!"
}

Response:
{
  "token": "eyJhbG...",
  "expiresAt": "2024-01-01T12:00:00Z",
  "username": "admin@acme.com",
  "role": "Admin",
  "tenantId": "...",
  "tenantName": "Acme Corp"
}
```

### JWT Claims

```json
{
  "sub": "user-guid",
  "tenantId": "tenant-guid",
  "role": "Admin",
  "username": "admin@acme.com"
}
```

### Role-Based Authorization

| Role      | Capabilities                                                |
|-----------|-------------------------------------------------------------|
| **Admin** | Full CRUD on all tenant tasks                               |
| **User**  | View tenant tasks, complete **only** tasks assigned to them |

```csharp
// Admin-only endpoint
[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<TaskDto>> CreateTask(...) { }

// Any authenticated user, but service enforces assignment rules
[HttpPatch("{id}/complete")]
[Authorize]
public async Task<ActionResult<TaskDto>> CompleteTask(...) { }
```

### Why JWT?

- **Stateless**: No server-side session storage required
- **Scalable**: Works across multiple API instances without session affinity
- **Self-contained**: Claims (tenantId, role) travel with the token
- **Industry standard**: Well-understood, extensive library support

---

## Database Design

### Entity Relationship

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│    Tenant    │       │     User     │       │   TaskItem   │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ Id (PK)      │◄──┐   │ Id (PK)      │◄──┐   │ Id (PK)      │
│ Name         │   │   │ TenantId(FK) │───┘   │ TenantId(FK) │───┐
└──────────────┘   │   │ Username     │       │ AssignedUser │───┤
                   │   │ PasswordHash │       │   Id (FK)    │   │
                   │   │ Role         │       │ Title        │   │
                   │   └──────────────┘       │ Description  │   │
                   │                          │ IsCompleted  │   │
                   │                          │ CreatedDate  │   │
                   └──────────────────────────│ CompletedAt  │   │
                                              └──────────────┘   │
                                                      ▲          │
                                                      └──────────┘
```

### Why GUIDs for primary keys?

- **API-friendly**: Can generate IDs client-side without round-trips
- **Merge-safe**: No conflicts when combining data from multiple sources
- **Non-sequential**: Doesn't expose record counts or creation order

Trade-off: Slightly larger storage and index size compared to integers. For this scale, the benefits outweigh the costs.

---

## API Endpoints

| Method | Endpoint                   | Auth   | Description                  |
|--------|----------------------------|--------|------------------------------|
| POST   | `/api/auth/login`          | Public | Authenticate and receive JWT |
| GET    | `/api/tasks`               | JWT    | List all tasks for tenant    |
| GET    | `/api/tasks/{id}`          | JWT    | Get single task              |
| POST   | `/api/tasks`               | Admin  | Create task                  |
| PUT    | `/api/tasks/{id}`          | Admin  | Update task                  |
| PATCH  | `/api/tasks/{id}/complete` | JWT*   | Mark task complete           |
| DELETE | `/api/tasks/{id}`          | Admin  | Delete task                  |
| GET    | `/api/dashboard/summary`   | JWT    | Get task statistics          |

*Users can only complete tasks assigned to them; Admins can complete any task.

### Optimized Query (Dashboard Summary)

SQLite doesn't support stored procedures. The dashboard summary uses an optimized EF Core query:

```csharp
var total = await query.CountAsync();
var completed = await query.CountAsync(t => t.IsCompleted);
return new TenantTaskSummary(total, completed, total - completed);
```

This generates efficient SQL:
```sql
SELECT COUNT(*) FROM Tasks WHERE TenantId = @tenantId;
SELECT COUNT(*) FROM Tasks WHERE TenantId = @tenantId AND IsCompleted = 1;
```

On SQL Server, this could be replaced with a stored procedure for single-query execution.

---

## Running the Application

### Prerequisites

- .NET 9 SDK
- Node.js 18+
- (Optional) Visual Studio 2022 for WPF

### Backend API

```bash
cd src/TaskManagement.Api
dotnet restore
dotnet run
```

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Database: `taskmanagement.db` (auto-created with seed data)

### Frontend (React)

```bash
cd client
npm install
npm run dev
```

- UI: http://localhost:5173

### Desktop (WPF)

```bash
cd desktop/TaskManagement.Desktop
dotnet run
```

Requires the API to be running.

### Demo Accounts

| Username         | Password     | Role  | Tenant     |
|------------------|--------------|-------|------------|
| admin@acme.com   | Password123! | Admin | Acme Corp  |
| user@acme.com    | Password123! | User  | Acme Corp  |
| admin@globex.com | Password123! | Admin | Globex Inc |

**Test tenant isolation**: Log in as `admin@acme.com`, note the tasks. Log out, log in as `admin@globex.com`—you should see completely different data.

---

## Testing

```bash
dotnet test
```

### Test Coverage

| Service             | Tests                                                          |
|---------------------|----------------------------------------------------------------|
| `AuthService`       | Valid login, invalid credentials, user not found               |
| `TaskService`       | Create task, complete task, tenant filtering, assignment rules |
| `DashboardService`  | Summary calculation                                            |

### Testing Philosophy

I focused on testing **business logic** rather than framework code. The tests verify:

1. **Tenant isolation**: Tasks are filtered correctly
2. **Authorization rules**: Users can only complete assigned tasks
3. **Service behavior**: Create, update, complete operations work correctly

```csharp
[Fact]
public async Task CompleteTask_WhenUserNotAssigned_ThrowsForbidden()
{
    // Arrange: User tries to complete someone else's task
    // Act & Assert: ForbiddenException thrown
}
```

---

## Technology Choices & Trade-offs

### Why SQLite?

**Decision**: Use SQLite for development, design for SQL Server compatibility.

**Reasoning**: SQLite eliminates setup friction—reviewers can run `dotnet run` without configuring a database server. The repository pattern and EF Core abstraction mean switching to SQL Server requires only a connection string change and migration.

**Trade-off**: No stored procedures in SQLite. The dashboard query uses EF Core LINQ that generates efficient SQL. For production, I would add a `GetTenantTaskSummary` stored procedure.

### Why Repository Pattern?

**Decision**: Abstract data access behind `ITaskRepository` and `IUserRepository`.

**Reasoning**:
- Services depend on interfaces, not EF Core directly
- Unit tests can mock repositories without an in-memory database
- Centralizes query logic (tenant filtering always happens in repository)

**Trade-off**: Additional abstraction layer. For a small application, this could be considered over-engineering. However, it demonstrates understanding of separation of concerns and testability.

### Why FluentValidation?

**Decision**: Use FluentValidation instead of Data Annotations.

**Reasoning**:
- Validation rules are testable
- Complex validation (cross-field, async) is cleaner
- Separates validation from DTOs

### Why AutoMapper?

**Decision**: Use AutoMapper for Entity → DTO mapping.

**Reasoning**: Reduces boilerplate mapping code. For complex mappings (like including `AssignedUsername` from a navigation property), explicit configuration keeps it maintainable.

**Trade-off**: "Magic" mapping can hide bugs. I use explicit `CreateMap` configurations to make mappings visible and testable.

### Why React + Vite (not Angular)?

**Decision**: React with Vite and TypeScript.

**Reasoning**: Faster development iteration with Vite's HMR. React's component model is simpler for a dashboard-style application. The assignment allowed either framework—I chose based on personal productivity.

### Why WPF + MVVM Toolkit?

**Decision**: WPF with CommunityToolkit.Mvvm over WinForms.

**Reasoning**: MVVM demonstrates understanding of UI patterns and data binding. The toolkit reduces boilerplate with source generators for `ObservableProperty` and `RelayCommand`. The desktop app is intentionally minimal—it proves API consumption from a native client without scope creep.

---

## Future Improvements

Given more time, I would add:

1. **Integration tests** with `WebApplicationFactory` proving cross-tenant isolation at the HTTP level
2. **SQL Server deployment** with actual stored procedure for dashboard summary
3. **Refresh tokens** for production-ready JWT lifecycle
4. **Audit logging** for compliance (who changed what, when)
5. **Azure deployment** with Key Vault for JWT secrets
6. **OpenAPI client generation** for type-safe frontend API calls

---

## CI/CD

### GitHub Actions

`.github/workflows/ci.yml` runs on every push:
- Restore dependencies
- Build solution
- Run tests
- Publish API artifact

### Azure Pipelines

`azure-pipelines.yml` provides equivalent functionality for Azure DevOps environments.

---

## Git Commit History

This repository demonstrates incremental development:

1. Initial solution structure and domain entities
2. EF Core configuration and migrations
3. JWT authentication implementation
4. Task service with tenant filtering
5. API controllers with authorization
6. React frontend with Material UI
7. WPF desktop client
8. Unit tests for business logic
9. Documentation and README

Each commit represents a logical unit of work, allowing reviewers to understand the development process.

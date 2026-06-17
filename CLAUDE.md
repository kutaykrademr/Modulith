# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Start infrastructure (PostgreSQL + pgAdmin + Mailpit)
docker compose up -d

# Dev secret (JWT signing key — NOT stored in the repo)
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)" --project src/Host/Modulith.Api

# Run the API (migrations applied automatically on startup)
dotnet run --project src/Host/Modulith.Api/Modulith.Api.csproj

# Build the entire solution
dotnet build Modulith.slnx

# Run integration tests (requires Docker — Testcontainers)
dotnet test
```

**Dev URLs:**
- API: `http://localhost:5116`
- Scalar (OpenAPI UI): `http://localhost:5116/scalar/v1`
- pgAdmin: `http://localhost:5050` (admin@modulith.dev / admin)
- Mailpit (email UI): `http://localhost:8025`

**Database schema is managed by EF Core Migrations** and applied automatically at startup (`Database.MigrateAsync()`). Migrations live in `src/Shared/Shared.Infrastructure/Persistence/Migrations` (DbContext is the migrations assembly; the API is the startup project). In Development, set `Database:ResetOnStartup=true` to drop and recreate from scratch.

```bash
dotnet ef migrations add <Name> \
  --project src/Shared/Shared.Infrastructure \
  --startup-project src/Host/Modulith.Api \
  --output-dir Persistence/Migrations
```

## Architecture

This is a **.NET 10 Modulith** — a modular monolith using vertical slice architecture per module with Clean Architecture layers inside each module.

### Project Structure

```
src/
├── Host/
│   └── Modulith.Api/          # Entry point — wires all modules together
├── Modules/
│   ├── Auth/                  # Authentication module
│   │   ├── Auth.Domain        # Entities, repository interfaces, domain services interfaces
│   │   ├── Auth.Application   # MediatR command/query handlers (one folder per use case)
│   │   ├── Auth.Infrastructure # DI registration, EF configs, repository impls, services
│   │   └── Auth.Presentation  # Minimal API endpoint mapping
│   └── User/                  # User profile module
│       ├── User.Domain
│       ├── User.Application
│       ├── User.Infrastructure
│       └── User.Presentation
└── Shared/
    ├── Shared.Contracts/      # Integration events (INotification), IUnitOfWork interface
    └── Shared.Infrastructure/ # ModulithDbContext, UnitOfWork implementation
```

### Key Architectural Decisions

**Single shared DbContext (`ModulithDbContext`):** All modules write to one PostgreSQL database via `ModulithDbContext`. Module EF configurations are auto-discovered at startup — `ModulithDbContext.OnModelCreating` scans all loaded assemblies ending in `"Infrastructure"` and applies `IEntityTypeConfiguration<T>` classes found there.

**DB schema isolation per module:** Tables are namespaced by schema (e.g., `auth.users`, `user.profiles`) using `builder.ToTable("users", "auth")` in EF configurations.

**Cross-module communication via MediatR integration events:** Modules never reference each other. `Shared.Contracts` holds `INotification` records (e.g., `UserRegisteredIntegrationEvent`) that the publishing module dispatches via `IPublisher` and consuming modules handle via `INotificationHandler<T>`. The handler runs in-process within the same transaction.

**Unit of Work for cross-module transactions:** When Auth registers a user and the User module creates a profile, both happen in a single DB transaction managed by `IUnitOfWork` (Begin → publish event → event handler adds to context → SaveChanges → Commit). The event handler does **not** call `SaveChanges` itself.

**Module registration pattern:** Each module exposes a static `AddXxxModule(IServiceCollection, IConfiguration)` extension that registers all its internals (repositories, MediatR, services). The Host `Program.cs` calls these. The Host only references `*.Presentation` and `Shared.Infrastructure` projects.

**Minimal API endpoints:** Each module has a `*Endpoints.cs` class with a `MapXxxEndpoints(IEndpointRouteBuilder)` extension. Endpoint groups use `/api/v1/{module}` prefix.

### Adding a New Module

1. Create `src/Modules/{Name}/` with Domain / Application / Infrastructure / Presentation projects.
2. Add EF entity configurations in Infrastructure using the module's own DB schema.
3. Register with `services.AddXxxModule(configuration)` in `Program.cs`.
4. Map endpoints with `app.MapXxxEndpoints()` in `Program.cs`.
5. For cross-module events: define `INotification` records in `Shared.Contracts`, publish via `IPublisher`, handle via `INotificationHandler<T>` in the consuming module's Application layer.

### Key Config Sections (`appsettings.json`)

| Section | Purpose |
|---|---|
| `ConnectionStrings:ModulithDb` | PostgreSQL connection string (override via `ConnectionStrings__ModulithDb` env var in prod) |
| `Jwt:Secret` | HMAC signing key. **Never committed** — user-secrets in dev, `Jwt__Secret` env var in prod (min 32 chars) |
| `Jwt` | Issuer, Audience, access/refresh token expiry |
| `Cors:AllowedOrigins` | Allowed CORS origins (empty ⇒ same-origin only) |
| `Email` | SMTP settings (Mailpit in dev on port 1025) |
| `App:BaseUrl` | Used to build email verification/reset links |
| `RateLimiting` | Fixed-window limits for `fixed`, `login`, `register` policies |
| `Database:ResetOnStartup` | Dev-only: drop & recreate DB before applying migrations |

`ConfigurationValidationExtensions.ValidateCriticalConfiguration` runs at startup and fails fast if `Jwt:Secret` (≥32 chars), issuer, audience, or the connection string are missing.

### JWT Claims

Tokens use raw claim names (`sub`, `email`, `name`) — `MapInboundClaims = false` is set. Use `user.FindFirstValue("sub")` to get the user ID in endpoints.

### Security model

- **Passwords:** BCrypt, workFactor 12. Login runs a dummy verify when the user is missing to avoid email-enumeration timing leaks (`IPasswordHasher.VerifyDummy`).
- **Opaque tokens** (refresh, email verification, password reset) are stored as **SHA-256 hashes**, never plaintext (`Auth.Domain.Services.TokenHasher`). The raw token is only ever sent to the user.
- **Refresh tokens:** rotated on every use. Replaying a revoked token triggers **reuse detection** — the whole token chain for that user is revoked (`RefreshTokenCommandHandler`).
- **Password reset** revokes all of the user's refresh tokens.
- **Transport/headers:** HSTS in non-dev, security headers on every response (`SecurityHeadersExtensions`), CORS policy from config.
- **Pipeline order** (`Program.cs`): SerilogRequestLogging → ExceptionHandler → SecurityHeaders → HttpsRedirection → CORS → RateLimiter → AuthN → AuthZ.

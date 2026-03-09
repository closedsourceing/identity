# Identity Server

A production-grade, standalone OpenID Connect / OAuth 2.0 identity server built with [OpenIddict](https://openiddict.com/) and ASP.NET Core 10.

## Architecture

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 10 (minimal hosting) |
| Auth protocol | OpenIddict 7 — OIDC / OAuth 2.1 |
| Persistence | Entity Framework Core 10 + PostgreSQL (Npgsql) |
| Schema | `appsarena_identity` database, `identity` schema |
| Testing | xUnit + WebApplicationFactory + SQLite (in-memory) |

## Supported OAuth Flows

| Flow | Notes |
|------|-------|
| Authorization Code + PKCE | Required for all interactive / browser clients |
| Client Credentials | Machine-to-machine (M2M) |
| Refresh Token | Rotated; short-lived access tokens |

## Getting Started

### Prerequisites

- .NET 10 SDK (`global.json` pins `10.0.102`)
- Docker / Docker Compose (for local PostgreSQL)

### Run Locally

```bash
# 1. Start the database
docker compose up postgres -d

# 2. Run the server (EF Core migrations run automatically on startup)
cd src/Identity.Server
dotnet run
```

The server starts on `http://localhost:5187` (HTTP) and `https://localhost:7235` (HTTPS) using the default `launchSettings.json`.

OIDC discovery: `http://localhost:5187/.well-known/openid-configuration`

### Configuration

The server reads configuration from `appsettings.json` overlaid by `appsettings.{Environment}.json` and environment variables.

#### Required Environment Variables (Production)

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;Database=appsarena_identity;Username=identity;Password=SECRET` |

#### Optional Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OpenIddict__TokenLifetimes__AccessTokenMinutes` | `60` | Access token lifetime in minutes |
| `OpenIddict__TokenLifetimes__RefreshTokenDays` | `14` | Refresh token lifetime in days |
| `OpenIddict__TokenLifetimes__AuthorizationCodeMinutes` | `5` | Authorization code lifetime |

#### Seeding Clients via Configuration

Add entries under `OpenIddict:Clients` in `appsettings.json` or via environment variables:

```json
{
  "OpenIddict": {
    "Clients": [
      {
        "ClientId": "my-spa",
        "DisplayName": "My SPA",
        "ClientSecret": null,
        "ConsentType": "implicit",
        "ClientType": "public",
        "RedirectUris": [ "https://app.example.com/callback" ],
        "PostLogoutRedirectUris": [ "https://app.example.com" ],
        "Permissions": [
          "ept:authorization",
          "ept:token",
          "ept:end_session",
          "gt:authorization_code",
          "gt:refresh_token",
          "rst:code",
          "scp:openid",
          "scp:profile",
          "scp:email",
          "scp:offline_access"
        ]
      }
    ]
  }
}
```

> **Note:** Client seeding is idempotent — existing clients are not overwritten on restart.

### Database Migrations

```bash
# Apply migrations (done automatically on startup, or manually):
dotnet ef database update --project src/Identity.Server

# Create a new migration:
dotnet ef migrations add <MigrationName> --project src/Identity.Server --output-dir Data/Migrations
```

## Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /.well-known/openid-configuration` | OIDC Discovery document |
| `GET /.well-known/jwks` | JSON Web Key Set |
| `GET/POST /connect/authorize` | Authorization endpoint |
| `POST /connect/token` | Token endpoint |
| `GET/POST /connect/userinfo` | UserInfo endpoint |
| `GET/POST /connect/endsession` | End-session (logout) endpoint |
| `POST /connect/introspect` | Token introspection |
| `POST /connect/revoke` | Token revocation |
| `GET /health` | Health check |
| `GET /account/login` | Login page |

## Testing

```bash
dotnet test
```

Tests run against an in-memory SQLite database via `WebApplicationFactory<Program>` — no external dependencies required.

### Test Coverage

- Discovery endpoint contract (issuer, endpoints, PKCE, grant types)
- JWKS endpoint format and key availability
- Authorization endpoint redirect-to-login for unauthenticated users
- Token endpoint negative paths (invalid grant type, unknown client, invalid code)
- Login page rendering

## Security Defaults

- PKCE required for all authorization code flows
- HTTPS enforced in production (disabled in Development/Test)
- Cookie `HttpOnly`, `SameSite=Lax`, `Secure` in production
- Short access token lifetime (60 min by default, configurable)
- No secrets committed — use environment variables or .NET User Secrets
- Never log raw tokens, authorization codes, or passwords

## Docker

```bash
# Build and run the full stack
docker compose up --build
```

The identity server image runs as a non-root user (`appuser:appgroup`).

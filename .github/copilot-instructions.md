# GitHub Copilot Instructions

This repository hosts a C# OpenIddict-based identity server. Prefer secure, standards-aligned, testable changes.

## Architecture
- Use ASP.NET Core minimal hosting or explicit Startup style already present in the repo.
- Keep domain logic out of controllers/endpoints.
- Use thin API endpoints, service-layer orchestration, and persistence abstractions.

## OpenIddict Rules
- Use OpenID Connect and OAuth 2.1 best practices.
- Prefer authorization code flow with PKCE for public clients.
- Avoid implicit flow.
- Issue only required scopes and claims.
- Keep token lifetimes short and configurable.
- Rotate signing keys and support key rollover.
- Use reference tokens only when revocation/introspection is required.

## Security
- Default to secure-by-default settings.
- Validate redirect URIs strictly.
- Enforce HTTPS in non-local environments.
- Never log secrets, tokens, authorization codes, or raw PII.
- Use ASP.NET Core Data Protection for key material where appropriate.
- Prefer defense-in-depth: input validation, anti-forgery where relevant, strict CORS.

## Data and Migrations
- Use EF Core migrations with clear names.
- Keep schema changes backward-compatible when possible.
- Seed OpenIddict applications/scopes idempotently.

## Code Style
- Use nullable reference types and async/await end-to-end.
- Accept CancellationToken in I/O boundaries.
- Prefer explicit DTOs over exposing entities.
- Keep methods short and single-purpose.

## Testing
- Add or update tests for behavior changes.
- Include integration tests for token, authorize, discovery, introspection, and revocation endpoints.
- Verify negative paths: invalid scope, invalid redirect URI, expired/reused code, invalid client auth.

## Operational Requirements
- Add structured logs with correlation IDs.
- Keep configuration in appsettings and environment variables.
- Document required env vars in README when adding new config.

## PR Expectations
- Explain security implications of auth/token changes.
- Include test evidence for protocol and regression scenarios.

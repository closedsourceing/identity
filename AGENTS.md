# AGENTS

## Primary Agent Goal
Build and maintain a production-grade C# identity server using OpenIddict with strong security defaults.

## Non-Negotiable Constraints
- Follow OIDC/OAuth standards and OpenIddict guidance.
- Prefer secure defaults over convenience.
- Never introduce flows/features that weaken client or token security.
- Every auth behavior change must include tests.

## Implementation Rules
- Keep endpoint handlers thin; move protocol logic to services.
- Use typed options for security-sensitive config.
- Add cancellation tokens to I/O methods.
- Do not break existing public contracts without migration notes.

## Done Criteria
- Compiles without warnings introduced by the change.
- Relevant tests added/updated and passing.
- Discovery metadata and key auth endpoints still conform.
- Security review checklist updated for auth/token changes.

# Add OpenIddict Client Registration

Create or update code to register a new OAuth/OIDC client in an idempotent startup seed routine.

## Requirements
- Use OpenIddict application manager APIs.
- Client type and consent type must be explicit.
- Redirect URIs must be exact and validated.
- Grant types and response types must be minimal and justified.
- Required scopes must be explicit.
- Secret handling must avoid hardcoded values.

## Output
- C# code changes.
- Any migration/config updates.
- Tests for success and invalid client configuration cases.

# Skill: OpenIddict Security Hardening

Use this skill when implementing auth, token, or client configuration changes.

## Checklist
- Enforce HTTPS and secure cookie settings outside local development.
- Ensure redirect URI validation is exact-match.
- Keep access token lifetime short and configurable.
- Verify refresh token reuse/rotation policy is intentional.
- Minimize claims in ID/access tokens.
- Do not log raw tokens, secrets, or authorization codes.
- Confirm signing key strategy supports rotation.

## Required Validation
- Test invalid redirect URI rejection.
- Test invalid/expired token behavior.
- Test scope overreach rejection.

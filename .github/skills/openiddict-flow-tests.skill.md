# Skill: OpenIddict Flow Tests

Use this skill when adding or changing OAuth/OIDC flow behavior.

## Must-Have Tests
- Authorization code + PKCE success path.
- Authorization code replay prevention.
- Client credentials success path (if enabled).
- Refresh token success and rejection paths.
- Discovery endpoint contract checks.
- JWKS endpoint availability/format checks.

## Negative Tests
- Invalid client authentication.
- Invalid scope request.
- Invalid redirect URI.
- Expired authorization code/token.
- Missing PKCE verifier/challenge mismatch.

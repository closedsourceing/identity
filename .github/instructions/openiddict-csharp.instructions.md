---
applyTo: "**/*.cs,**/*.csproj,**/appsettings*.json,**/*.http"
---

When generating or editing code in this repository:

1. Treat this project as an OpenIddict identity provider in ASP.NET Core.
2. Favor authorization code + PKCE, client credentials, and refresh token flows only when explicitly required.
3. Keep token issuance minimal (least privilege scopes/claims).
4. Enforce strict redirect URI validation and reject wildcard redirects.
5. Ensure endpoint changes preserve compatibility with OIDC discovery and JWKS metadata.
6. Use async APIs, nullable reference types, and explicit DTO contracts.
7. Add/update tests with positive and negative auth scenarios.
8. If adding config, bind via strongly typed options and document env vars.
9. Avoid adding dependencies when built-in ASP.NET Core/OpenIddict features are sufficient.

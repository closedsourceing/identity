# Review Authentication/Token Change

Review a proposed change touching authentication, token issuance, scopes, claims, endpoints, or key management.

## Review Focus
- Protocol correctness (OIDC/OAuth behavior)
- Security regressions
- Backward compatibility and migration impact
- Negative-path coverage
- Observability (safe logs/metrics)

## Output Format
1. Findings ordered by severity.
2. Exact file/line references.
3. Missing tests and recommended test cases.
4. Brief summary of merge risk.

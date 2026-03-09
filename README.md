# identity

## Copilot and Agent Setup

This repository includes project-specific guidance for GitHub Copilot and coding agents.

### Added Files
- `.github/copilot-instructions.md`
- `AGENTS.md`
- `.github/instructions/openiddict-csharp.instructions.md`
- `.github/prompts/add-openiddict-client.prompt.md`
- `.github/prompts/review-auth-change.prompt.md`
- `.github/skills/openiddict-security-hardening.skill.md`
- `.github/skills/openiddict-flow-tests.skill.md`
- `.github/skills/openiddict-efcore-persistence.skill.md`

### Suggested Usage
- Keep architectural and security constraints in `.github/copilot-instructions.md`.
- Use `AGENTS.md` for always-on agent behavior and done criteria.
- Use `.github/instructions/*.instructions.md` for language/file-targeted instructions.
- Use `.github/prompts/*.prompt.md` as reusable task starters.
- Use `.github/skills/*.skill.md` as checklists for common OpenIddict tasks.
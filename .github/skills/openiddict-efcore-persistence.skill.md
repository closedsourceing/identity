# Skill: OpenIddict EF Core Persistence

Use this skill for schema, migration, and seed changes.

## Rules
- Keep migrations small and named by intent.
- Ensure startup seeding is idempotent.
- Avoid destructive migration steps without rollback notes.
- Keep index/constraint updates explicit.

## Verification
- Fresh database bootstraps successfully.
- Existing database migrates without data loss.
- Seed reruns without duplicates.

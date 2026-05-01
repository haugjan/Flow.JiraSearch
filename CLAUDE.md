# Flow.JiraSearch — Claude Instructions

## Documentation

Developer documentation lives in [`docs/`](docs/README.md). Read the index there first; it links to:

- [Architecture](docs/architecture.md) — plugin layout, runtime composition, and the custom JQL DSL.
- [Development](docs/development.md) — build, test, and live dev-loop workflow.
- [Release process](docs/release-process.md) — how `plugin.json:Version` drives auto-release.
- [Known issues](docs/known-issues.md) — current build/release tooling inconsistencies worth fixing.

End-user documentation (installation, configuration, search-operator reference) lives in the [top-level README](README.md).

## Branch naming

The global `feature/firefly/OPA-…` / `hotfix/firefly/SUP-…` rules in `~/.claude/CLAUDE.md` are scoped to firefly work and do **not** apply to this personal repo. Use short `feature/<topic>` or `fix/<topic>` branch names. Commit subjects are plain imperative sentences with no ticket prefix.

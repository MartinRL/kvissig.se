---
status: Accepted
type: operations
created: 2026-06-21
revised:
---

# ADR 010: GitHub for Repository Hosting

## Context
The code needs a remote host for version control, collaboration, and as the trigger surface
for CI/CD. This is a vendor choice with no bearing on the system's architecture.

## Decision
Host the repository on **GitHub**.

## Rationale
- **Ubiquitous and zero-cost** for a public/personal project; familiar workflow.
- **Native CI/CD** via GitHub Actions (ADR 011) — no separate pipeline service to wire up.
- **First-class fly.io integration** (`superfly/flyctl-actions`) for deploys from Actions.

## Consequences
- Operational dependency on GitHub for hosting and as the CI/CD trigger.
- **Reversible.** Git is portable; the repository can move to another host with only the
  Actions workflows (ADR 011) needing a rewrite.

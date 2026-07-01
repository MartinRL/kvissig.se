---
status: Accepted
type: operations
created: 2026-06-21
revised:
---
# ADR 011: GitHub Actions for CI/CD

## Context
With the repository on GitHub (ADR 010) and hosting on fly.io (ADR 009), we need automated
build/test on changes and automated deploy on `main`. The build is non-standard: .NET 11
**preview** (pinned in `global.json` — see ADR 006), so the runner must be able to resolve a
preview SDK.

## Decision
Use **GitHub Actions** for CI and CD, in two workflows under `.github/workflows/`:

- **`ci.yml`** — on every pull request and push to `main`: `actions/setup-dotnet` with
  `dotnet-version: 11.0.x` + `dotnet-quality: preview`, then `dotnet build -c Release` and
  `dotnet test -c Release --no-build`.
- **`deploy.yml`** — on push to `main`: `superfly/flyctl-actions/setup-flyctl` then
  `flyctl deploy --remote-only`, authenticated with the `FLY_API_TOKEN` secret. The container
  (.NET 11 preview image) builds on **fly's remote builder**, so this job needs only flyctl,
  no .NET SDK. `concurrency: deploy-group` prevents two deploys running at once.

## Rationale
- **In the box with the repo host** (ADR 010) — no third-party CI service to manage.
- **Preview SDK is resolvable** via `dotnet-quality: preview`, matching the `global.json` pin.
- **Remote-only build** keeps the deploy job tiny and consistent with the production image,
  and serialized deploys (`concurrency`) suit the single-instance host (ADR 009).

## Consequences
- Operational dependency on GitHub Actions and the `FLY_API_TOKEN` secret.
- If `setup-dotnet` can't resolve the exact preview build, fall back to
  `dotnet-install.sh -Channel 11.0 -Quality preview` (noted inline in `ci.yml`).
- **Reversible.** The pipeline could move to any CI that can run a preview SDK build and
  `flyctl deploy`.

## Related
- ADR 009 — the fly.io host this pipeline deploys to.
- ADR 010 — the repository host these workflows run on.

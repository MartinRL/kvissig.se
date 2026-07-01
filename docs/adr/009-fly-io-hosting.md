---
status: Accepted
type: operations
created: 2026-06-21
revised:
---
# ADR 009: fly.io for Hosting

## Context
The app needs somewhere to run in production. ADR 001 fixes the *topology*: event sourcing
**in-memory**, no database — game state lives in a single process and is lost on restart
(acceptable for short-lived, same-room games). That topology is the architectural constraint;
this ADR only records *which provider* satisfies it. The runtime is also unusual: .NET 11
**preview** (net11.0 + C# preview union types, pinned in `global.json` — see ADR 006), which
no managed App-Service-style runtime offers out of the box, so we ship a container baking the
nightly preview SDK/runtime (`src/MerEllerMindre.Web/Dockerfile`).

Hard requirements:

- **Single stateful instance.** In-memory state (and the Data Protection keyring) means no
  horizontal scaling and the instance must not sleep mid-game (ADR 001).
- **Run an arbitrary container** so the .NET 11 preview image can run as-is.
- **Minimal ops, low cost** for a personal project; EU/Stockholm region for the audience.

## Decision
Host on **fly.io**, deploying the container from `fly.toml`:

- `app = "kvissig"`, `primary_region = "arn"` (Stockholm).
- **Exactly one instance:** `min_machines_running = 1`, `auto_stop_machines = false`
  (in-memory state must not sleep), `auto_start_machines = true`. No horizontal scaling — this
  is the operational expression of ADR 001's topology, not a new architectural decision.
- Builds the Dockerfile (`[build].dockerfile`); `internal_port = 8080`, `force_https = true`,
  health check on `/healthz`.
- Small VM: `shared-cpu-1x`, `256mb` — sized for same-room traffic.

## Rationale
- **Runs our container natively**, so the .NET 11 preview image needs no provider-specific
  runtime support.
- **First-class single-instance, no-sleep config** maps directly onto the in-memory
  constraint (`auto_stop_machines = false`, `min_machines_running = 1`).
- **Cheap and low-ops** for one small machine; Stockholm region keeps latency low for the
  (Swedish) audience.

## Consequences
- Single point of failure / no zero-downtime deploy — acceptable; a restart only drops
  in-flight games, which ADR 001 already accepts.
- Operational dependency on fly.io (small fixed cost).
- **Reversible.** Any container host with a single always-on instance could replace fly.io;
  only `fly.toml` and the deploy step (ADR 011) are provider-specific.

## Related
- ADR 001 — the in-memory topology this hosting must preserve (do not duplicate it here).
- ADR 011 — CI/CD that builds the image and deploys it here.

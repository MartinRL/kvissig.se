---
status: Accepted
created: 2026-06-20
revised:
---

# ADR 008: Plausible Cloud for Analytics

## Context
The site shipped with no analytics (`specs/tasks.md` had an open `analytics (GA2?)` TODO).
We want two things and nothing more:

1. **Traffic / vanity metrics** — pageviews, referrers, countries, devices.
2. **A gameplay funnel** — create → join → start → complete, to see drop-off.

Question-calibration (per-card difficulty) and ops dashboards are explicitly out of scope
for now.

Hard constraints for *this* project:

- **No consent banner.** kvissig.se is a small personal project; a cookie/GDPR banner is
  disproportionate friction for the audience (family & friends) and the value. This is the
  single deciding requirement.
- **Minimal ops.** No appetite to run extra infrastructure for analytics.
- **Private project, separate identity.** kvissig.se is unrelated to chronoshub.io and its
  existing OpenPanel account — that account must never be reused here.

## Options considered

### 1. Google Analytics 4 (the original `GA2?` note)
- **Pros:** free, ubiquitous, powerful, built-in funnel/exploration reports.
- **Cons:** sets cookies and sends IP + identifiers to Google in the US → **cannot be made
  cleanly banner-free** under GDPR. Heavy script, data-sharing model misaligned with a
  privacy-friendly personal site. **Rejected** — fails the no-banner constraint.

### 2. Plausible — self-hosted
- **Pros:** cookieless/GDPR-clean like the cloud version; full data ownership; no per-event
  cost.
- **Cons:** the stack is app + PostgreSQL + ClickHouse (~2 GB RAM host). That is more
  infrastructure than the game itself (single fly.io instance, in-memory state, no DB by
  design — ADR 001). Disproportionate ops burden for this traffic. **Rejected** — fails the
  minimal-ops constraint.

### 3. Other privacy-first SaaS (Fathom, Simple Analytics, OpenPanel, …)
- **Pros:** comparable cookieless models; some are also banner-free.
- **Cons:** no decisive advantage over Plausible for our needs; OpenPanel specifically is
  entangled with the unrelated chronoshub.io account we must keep separate. No reason to
  prefer them. **Not selected.**

### 4. Plausible Cloud (chosen)
- **Pros:** cookieless, stores no personal data, EU-hosted, GDPR-clean → **no consent
  banner**. One script tag, zero ops. Custom events available on every plan, exposed as
  goals (counts + drop-off ratios). Lightweight script.
- **Cons:** paid after the free trial (small fixed cost); the *visual* Funnel builder is a
  Business-plan feature (we don't need it — the four goal counts give the funnel); a
  third-party dependency for the data.

## Decision
Use **Plausible Cloud**.

- **Traffic:** the site-specific Plausible snippet in the global `<head>`
  (`Components/MainLayout.razor`) auto-tracks pageviews.
- **Gameplay funnel:** four custom goals — `game_created`, `player_joined`, `game_started`,
  `game_completed` — fired **server-side**, once each, only on command success, from the
  four endpoints in `GameEndpoints.cs`, via Plausible's keyless Events API
  (`POST https://plausible.io/api/event`) forwarding the visitor's `User-Agent` and
  `X-Forwarded-For`. The Plausible site domain comes from config (`Plausible:Domain`).
- The Events POST is **no-op outside Production** so local runs don't pollute stats.

## Rationale
- **No-banner requirement decides it.** GA4 can't satisfy it cleanly; Plausible is
  cookieless and personal-data-free by design.
- **Cloud over self-host** because the self-hosted stack dwarfs the app's own footprint.
- **Server-side funnel, not client-side.** In-game screens are re-rendered by the htmx 2 s
  poll on `/games/{code}/state` (ADR 003); a client hook would re-fire on every poll tick
  and inflate counts. Firing server-side on command success counts each transition exactly
  once.
- **Fire-and-forget, no retry/queue.** At this traffic a dropped event is just a missing
  count — resilience would be over-engineering. Add it only if events measurably drop.

## Consequences
- One new typed `HttpClient` (`PlausibleClient`) and one config key (`Plausible:Domain`);
  no domain changes — the functional core and all tests are untouched (shell-only).
- `GameApplicationService.RunProgressionGear` now returns `Result<GameEvent[]>` so the
  `/next` endpoint can detect the `GameEnded` event and fire `game_completed`.
- **No consent banner needed**; the site keeps only its existing essential cookies.
- Operational dependency on Plausible Cloud (and its small fixed cost) for analytics.
- Reporting reads as four counts (create N → join → start → complete K); drop-off is the
  ratios between them. The visual Funnel builder is deliberately not used.

## Future hooks (not built)
- Client-side funnel events — rejected (polling over-count).
- Question-calibration analytics — later a `question_scored` event with props
  `{ packId, cardId, directionCorrect }`.
- Delivery resilience (queue/retry) — only if events measurably drop.

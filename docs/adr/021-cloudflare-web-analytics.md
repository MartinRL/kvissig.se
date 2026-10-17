---
status: Accepted
type: operations
created: 2026-10-17
supersedes: 008
---
# ADR 021: Cloudflare Web Analytics instead of Plausible Cloud

## Context
ADR 008 chose Plausible Cloud for traffic plus a server-side gameplay funnel. The free
trial ended and the dashboard now requires a paid plan; it showed 0 visitors. The
no-consent-banner and minimal-ops constraints from ADR 008 still hold. The DNS for
kvissig.se already lives in a free Cloudflare account (grey cloud, traffic goes straight
to fly.io).

## Decision
Use **Cloudflare Web Analytics** (free) with its JS beacon in the global `<head>`
(`Components/MainLayout.razor`). Drop the gameplay funnel: `PlausibleClient`, its typed
`HttpClient`, the `Plausible:Domain` config key and the four `Track` calls are removed.

## Rationale
- Free, cookieless, no personal data stored → still **no consent banner**.
- Zero ops and no new vendor account: the Cloudflare account exists for DNS.
- The funnel was never read while the trial ran; paying to keep it is not justified yet.

## Consequences
- Pageviews, referrers, countries, devices and Core Web Vitals only; data is sampled.
- No custom events: create → join → start → complete drop-off is no longer measured.
  Cards that wanted events (share clicks, "buy as app") need a new decision first.
- The beacon is client-side and counts htmx page loads, not the 2 s state polls (they are
  fetches, not navigations).
- `RunProgressionGear` keeps returning `Result<GameEvent[]>` (ADR 008); harmless, unused.

## Future hooks (not built)
- Funnel back: a free event-capable tool (e.g. Umami Cloud) or a paid Plausible plan,
  reusing the server-side firing points ADR 008 describes.

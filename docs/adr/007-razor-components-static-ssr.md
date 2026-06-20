---
status: Accepted
created: 2026-06-14
revised:
---

# ADR 007: Razor Components in Static SSR as the Renderer

## Context
The UI/UX direction is locked (the "Lekfull kortlek" design, `prototype/pico/`), but
nothing was wired into the app — the whole Web shell (`src/MerEllerMindre.Web`) was a stub.
We need to pick a *server-side renderer* that turns projected view-models into HTML for both
full pages and the HTML fragments htmx swaps in.

ADR 003 already locked the interaction model: **htmx + 2 s polling**, no WebSockets/SSE.
CLAUDE.md listed "Blazor" under *Forbidden*. On closer reading that blanket ban was an
over-simplification: ADR 003 rejected **Blazor Server** specifically — its stateful
WebSocket *circuit* — as an alternative to htmx polling. It did not evaluate, and had no
reason to reject, server-rendered Razor Components with no interactivity.

Candidate renderers considered:

1. **Razor Pages / MVC views** — works, but a second templating model alongside nothing else.
2. **Razor Slices** (third-party) — lightweight, but an external dependency, and a higher
   risk surface on the .NET 11 *preview* SDK we run (see ADR 006 for why we are on preview).
3. **Razor Components in static SSR** — `.razor` components rendered with
   `RazorComponentResult<TComponent>`, **no** interactive render mode.

## Decision
Use **Razor Components in static server-side rendering** as the renderer.

- `builder.Services.AddRazorComponents()` — **without** `.AddInteractiveServerComponents()`.
- `@rendermode` is **never** set. There is therefore **no circuit, no WebSocket, and no
  `blazor.web.js`** shipped to the client. The server renders HTML and the response ends.
- Endpoints return `new RazorComponentResult<TComponent>(new { Param = value })`; parameters
  flow in as `[Parameter]` properties. A full page vs. a fragment is simply *which* component
  is returned — full-page components carry a `<!DOCTYPE html>` shell (via `MainLayout`),
  fragment components (`PlayersList`, `OpenLobbyForm`) render bare markup.
- All interactivity is htmx: forms POST, and a `hx-trigger="every 2s"` poll GETs an HTML
  fragment (ADR 003). The renderer choice does not touch the interaction model.
- Static assets are served with **`UseStaticFiles()` + `wwwroot/`** (not `MapStaticAssets`,
  which is coupled to `MapRazorComponents` and misbehaves outside it).

## Rationale
- **No constraint violated.** No circuit, no WebSocket, no client runtime — the thing ADR
  003 rejected (Blazor Server) is exactly what we do *not* enable. Static SSR is "render HTML
  and stop", fully compatible with htmx polling.
- **In the box.** Razor Components SSR ships with the `Microsoft.NET.Sdk.Web` SDK — no NuGet
  package, the lowest dependency risk on the .NET 11 preview SDK (vs. third-party Razor Slices).
- **Ergonomic.** Layouts, components, and `[Parameter]` typing match the team's Blazor
  familiarity, and components only ever see projected view-models — never raw `GameState`.
- A smoke test (`AddRazorComponents` + a trivial component returned from a temporary
  `/_smoke` route) confirmed it builds and renders on the preview SDK with
  `TreatWarningsAsErrors=true` before further work; the route was then removed.

## Consequences
- CLAUDE.md's *Forbidden* entry is refined: **Blazor Server and interactive render modes
  (the WebSocket circuit) are forbidden; static SSR with Razor Components is allowed.**
- Components must stay free of interactive features (`@onclick`, `EditForm` round-trips, JS
  interop). Interaction is htmx attributes in the markup, full stop.
- Full page vs. fragment is a component-selection decision in the endpoint, keeping the
  core/shell seam thin.

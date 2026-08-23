---
status: Accepted
type: architecture
created: 2026-08-23
---

# ADR 019: Game screens render through a runtime xm interpreter

## Context
ADR 016–018 made the domain stratum a build artifact of the emlang spec. The UX layer
was still 3× hand-written Razor per game: every screen re-stated what
`specs/blindbudet.xm.yaml` already said (which views compose which surface, in what
salience tier, with which commands). xmlang v0.2 (`during:`, `self:`, nested labels,
version key) made the spec precise enough to be load-bearing, and the D0
characterization suite (`AuctionEndpointsTests`) gave a parity oracle: semantic markers
(labels, CSS classes, ordering), not full HTML.

SDUI prior art (Adaptive Cards, DoorDash/Airbnb write-ups) names the failure modes:
inner-platform syntax creep, per-element render fallbacks, and spec/renderer version
skew. All three are designed out below.

## Decision
Blindbudet's in-game screens render through a **runtime interpreter** over a **closed,
hand-built component vocabulary**. `XmCatalog` parses and lints the spec pair at
startup and **throws on any lint error** (the QuestionPackCatalog philosophy: fail the
deploy, never a render). The spec never reaches the browser; spec + interpreter deploy
atomically, deleting the version-skew class.

The stack (per stratum):

- **Spec** — `specs/blindbudet.xm.yaml` (xmlang 0.2) + the emlang event model. Owns
  surface composition, field order, salience tiers, labels, personas, `during:`/`for:`.
- **Engine (game-agnostic)** — `Xm/Fields.cs` (closed Field union: Text, Roster,
  Table, Qr, Steps), `Xm/RenderModel.cs`, `Components/Xm/*` (SurfaceRenderer,
  FieldBlock, PlayerRoster, ScoreTable, QrPanel, ActionForm, KeypadInput,
  CommandDefaultPage). ~450 LOC. Game-name checks in `Components/Xm/` are forbidden;
  a new Field kind or component is a reviewed, plan-level event — special cases either
  become vocabulary or the surface opts out to hand-written Razor, never new spec
  syntax.
- **Residue (per game)** — `Presentation/AuctionSurfaces.cs` (267 LOC): the fine
  screen selector (deliberately inexpressible in `during:`×`for:`, xm v0.2 finding 8),
  pure view materializers `(state, labels) → FieldBag`, and the command bindings
  (route table, AskNextLot/EndAuction mutual exclusion). The interpreter renders the
  **form**; endpoints, binding records and Decider dispatch stay hand-written.
- **Presence contract** — the xm owns field order and tier; the materializer owns
  presence. A field absent from the bag is unit-testable judgment, not a fallback.
- **Defaults contract** — bare (uncomposed) commands like OpenAuction/JoinAuction stay
  reachable; `CommandDefaultPage` is the transformer-defined form.

Cut-over: the D0 suite ran green against BOTH renderers via an `XmRenderer:Blindbudet`
flag, a manual two-participant Everest E2E (overbid ✗, shared win, descending
slutställning) passed on the xm renderer, then the flag, 8 hand-written screens,
`AuctionScreens.cs` and the screen Vms were deleted in one revertable commit
(−646 lines). The catalog page stays hand-written (SEO residue).

## Tripwire evaluation (mandated by the campaign plan)
**Smaller:** BB-specific presentation went from ~560 LOC (8 screens + AuctionScreens +
Vms) to 267 (AuctionSurfaces). **Better-tested:** screen selection and every formatting
judgment (sv-SE money, winner-line plurals, rank ordering) moved from untestable Razor
into pure functions covered by the 13-test characterization suite plus selector unit
tests; before D0, BB had zero web tests. **Verdict: pass — campaign continues to MEM/TTT.**

Honest ledger: BB alone is net positive LOC (engine ~450 + `src/Xmlang` ~500 amortize
across games). The currency is markup written once instead of 3×, judgment as pure
functions, and game #4 ≈ an xm file + ~250 LOC residue.

## Consequences
- A label/copy change is a spec edit; a new surface is a spec edit + a materializer.
- A spec typo is a startup crash in CI, not a broken screen in production.
- MEM and TTT roll over next (new idioms DirectionPicker/DifferenceSlider/ItemField are
  the expected vocabulary additions); the EM view-rename decision lands at MEM.
- Rollback of the cut-over = revert one commit; the engine is inert without callers.

## Postscript (2026-08)

`src/Xmlang` was extracted to github.com/MartinRL/xmlang and is consumed as the
NuGet package `Xmlang` (CLI: `Xmlang.Cli`, global tool `xm`).

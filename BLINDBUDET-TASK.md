# Ralph task — Blindbudet (spel #2) hela stacken från spec till spelbart

Build **Blindbudet** — a sealed-bid auction, MEM's mekaniska SYSTER-tvilling — from the
existing spec to a playable game: domain + GWT-tests + web-shell + a tiny mini-lott CSV.
Terminate on a self-verifying DONE: green `dotnet test` (incl. a NEW architecture-contract
test binding the blindbudet spec to the auction domain) + the marker file `BLINDBUDET-DONE`.

Source of truth = `specs/blindbudet-event-model.yaml` (lint-green, content already spiked).
Progress spår = `specs/blindbudet-tasks.md` (check off `[x]` as each piece lands).

## Game model (from the spec — do not re-derive, just implement)
Sealed first-price auction: each round a LOT with a hidden `trueWorth` is shown; every player
places ONE hidden bid; once all bids are in the System reveals — **highest bid wins, winner
pays their own bid**. `profit = winner ? round(trueWorth − pricePaid) : 0`; overbid → negative
= vinnarens förbannelse. **HÖGST total vinner — MOTSATT MEM (lägst).** Tie on the top bid =
earliest `BidPlaced` in the log (bids map folded in stream order → first top bidder wins). NO
budget in v1.

## Project structure (ponytail: no premature abstractions, n=2)
- NEW project pair **`Blindbudet.Domain`** + **`Blindbudet.Domain.Tests`** in the solution,
  parallel to the MerEllerMindre projects (MEM's name must not house game #2).
- **Reuse only the genuinely identical** via `<ProjectReference>` to `MerEllerMindre.Domain`:
  the `Result<T>` union (Ok/Err) + `QuestionPackCsvParser`. NO new "Kernel" module — extract
  first at game #3 if the pattern repeats.
- Web: NEW Blindbudet screens/endpoints in EXISTING `MerEllerMindre.Web` (it IS the kvissig.se
  chrome for both games) — parallel to MEM's GameEndpoints, sharing layout / HTMX-polling /
  in-memory event-store pattern.

## Build order per iteration (spec-first, same discipline as `.claude/ralph-prompt.md`)
1. **Spec** — `specs/blindbudet-event-model.yaml` is the source; only touch it if a gap surfaces
   during the build (re-lint if emlang is on PATH). Content is already spiked.
2. **Domain** (`Blindbudet.Domain`), records/unions mirrored on MEM:
   - `union AuctionCommand` (OpenAuction, JoinAuction, StartAuction, PlaceBid, RevealLot,
     AskNextLot, EndAuction)
   - `union AuctionEvent` (AuctionOpened, PlayerJoined, AuctionStarted, BidPlaced, LotRevealed,
     RoundScored, NextLotStarted, AuctionEnded) — each carries its OWN explicit `*At`, no shared base
   - `union AuctionError` (parameterless markers per spec: AuctionPackNotFound, AuctionNotFound,
     AuctionAlreadyStarted, NameAlreadyTaken, NotEnoughPlayers, BidNegative, AlreadyBid,
     LotAlreadyResolved, NotAllBidsIn)
   - `AuctionState` (`AuctionPhase notCreated|lobby|started|ended`, `IReadOnlyList<LotRound>`),
     `Lot(Description, TrueWorth decimal, Unit)`, `LotRound`, `AuctionContext`
     (NewGuid/Now/FindPack/… — mirror MEM's GameContext form).
   - All public types records; collections `IReadOnlyList<T>`; per-player maps keyed by playerId.
3. **Decider**: `Evolve`/`Decide` exhaustive switches, no default arms, business failures via
   `Result<T>` — never throw. `RevealLot`: highest bid wins, pays its own bid,
   `profit = round(trueWorth − pricePaid)`; tie = earliest `BidPlaced`. Total-score fold;
   `AuctionEnded` folds finalScoreboard/winnerIds (**highest wins**).
4. **Projections**: pure `AuctionState → View` fns for the spec view slices (catalog, lobby,
   lot, waiting/outstanding bids, round results, auction progress, final standings).
5. **GWT tests** (`Blindbudet.Domain.Tests`): implement ALL `tests:` from the spec. Test name =
   deterministic transform of the emlang name.
6. **Architecture-contract test** (NEW `BlindbudetArchitectureTests`): parse
   `specs/blindbudet-event-model.yaml` (`c:`/`e:`/`x:`) and contract-check against the auction
   unions — mirror of MEM's `ArchitectureTests`. The Stop-hook already runs all
   `ArchitectureTests`, so this becomes an automatic fitness gate overnight.
7. **Web-shell**: Blindbudet endpoints + Razor screens in `MerEllerMindre.Web` (lobby / join /
   bid number-field / reveal list / scoreboard), reusing MEM's shell pattern. Auction catalog
   loaded via the same CSV-catalog pattern.
8. **Mini-lott CSV**: one small `blindbudet-*-mini.csv` (columns `beskrivning;santVärde;tema;enhet`,
   sv-SE `;`/`,` + BOM) with a handful of REAL demo lots so the game is playable. NO full
   pipeline, NO new subagents.

## Hard rules
- **SYSTER-decider — NEVER touch MEM's Decider/spec/tests.** Only additive sister artifacts +
  Web additions. MEM's tests must stay untouched and green (proof MEM's decider wasn't touched).
- **HÖGST total vinner** — the opposite of MEM. Keep the two winner rules strictly apart in code
  AND copy; `winnerIds` = all players tied at the HIGHEST total.
- **The union runtime-type trap applies.** The union's runtime type is the union
  (`Result<T>`, `AuctionEvent`), NOT the case type. Use CONCRETE `is`-patterns:
  `result is Ok<AuctionEvent[]> ok`, `err.Error is AuctionNotFound`,
  `events.Where(e => e is BidPlaced bp …)`. NEVER a generic `is T` (falls back to isinst, never
  matches). Test helpers take concrete case types, not generics.
- **The word "gratis" må ALDRIG förekomma anywhere** (UI, copy, schema, docs). Lint at the end:
  Grep `-i gratis` over `src` → 0 hits.
- **ponytail — no `GameEngine<T>` abstraction (n=2).** Shortest working diff; reuse only the
  genuinely identical; no interface-with-one-impl, no factory, no config for constants.
- Author the mini CSV from REAL entities/figures only — never invent facts.

## Each iteration
1. `dotnet build` then `dotnet test`. Read failures; fix the ROOT CAUSE — never bypass a check,
   never `--no-verify`, never delete a failing test to make it pass.
2. Advance the build order; check off `specs/blindbudet-tasks.md` as pieces land.
3. Keep the diff additive; MEM projects/tests untouched. Commit so a limit hit is resumable.

## Stop
Write a file `BLINDBUDET-DONE` (and output the text `BLINDBUDET-DONE`) ONLY when the whole stack
stands AND `dotnet test` is fully green (MEM + new Blindbudet projects, incl.
`BlindbudetArchitectureTests`) AND `Grep -i gratis` over `src` = 0 hits.

## Escape hatches
- Stuck two iterations in a row (same failure) → change strategy, don't repeat; if still stuck
  write a file `RALPH-BLOCKED` with the blocker.
- A genuine spec gap that blocks the build → fix the smallest spec change, re-lint, note it in
  `specs/blindbudet-tasks.md`, continue. Never invent game rules beyond the spec.
- On usage-limit interruption: the loop stops; files + git are preserved → restart resumes from
  the checklist.

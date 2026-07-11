# Tänk Till Tusen — implementation checklist

Third sister-game for kvissig.se (Countdown / *Le compte est bon*). Own event-sourced Decider
beside MEM and BlindBudet; generated puzzles, hard 45 s countdown, LOWEST total wins.

## Phase 0 — prototypes (user picks)
- [x] Rename `prototype/` → `prototypes/`
- [x] Three solve-screen variants under `prototypes/tank-till-tusen/` + `solve-compare.html`
- [x] Link from `prototypes/index.html`
- [x] User picked **v2 · Räknartejp** → becomes `PuzzleScreen.razor`

## Spec (emlang)
- [x] `specs/tank-till-tusen-event-model.yaml` — lints `OK (no issues found)`

## Domain — `src/TankTillTusen.Domain/`
- [x] `Puzzles.cs` — Operator/Step/Solution/Puzzle, `SolutionValidator`, `Solver`, `PuzzleGenerator`
- [x] `Commands.cs` — `union TankCommand`
- [x] `Events.cs` — `union TankEvent`
- [x] `Errors.cs` — `union TankError` (9 markers)
- [x] `State.cs` — `TankPhase`, `Player`, `PuzzleRound`, `ScoreboardEntry`, `TankState` + derived
- [x] `Decider.cs` — Evolve/Decide/Fold, `TankContext`, `Result<T>`, `CountdownSeconds=45`, `RoundCount=5`
- [x] `Projections.cs` — pure State→View for the 6 screens

## Tests — `src/TankTillTusen.Domain.Tests/` (55 green)
- [x] `TankArchitectureTests` — records-only, readonly collections, Decider total/sync, no
      reflection/dynamic, forbidden price-word nowhere, spec↔domain contract (both directions)
- [x] `DeciderTests` — GWT from spec `tests:`
- [x] `EvolveTests`, `ProjectionTests`
- [x] `SolutionValidatorTests` (replay trust-boundary), `SolverTests` (solver + generator property)

## Web shell — `src/MerEllerMindre.Web`
- [x] `TankEndpoints.cs` — routes under `/tank-till-tusen/*`
- [x] `Infrastructure/TankApplicationService.cs` — store+repo+service+gears in one (score gear on
      AllSolutionsIn||DeadlinePassed, next/end gear)
- [x] `Presentation/TankScreenModels.cs` + `TankScreens.cs` (primitive-only VMs, screen selector)
- [x] `Components/TankTillTusen/*.razor` — Catalog, HostForm, JoinForm, Shell, LobbyHost/Player,
      PuzzleScreen (v2 Räknartejp + inline JS countdown, posts steps[]+answerIndex), Waiting,
      RoundResults, Standings

## Wiring
- [x] `Program.cs` — register `TankApplicationService` + `app.MapTankEndpoints()`
- [x] `MerEllerMindre.Web.csproj` — ProjectReference to `TankTillTusen.Domain`
- [x] `.slnx` — added Domain + Domain.Tests projects
- [x] sitemap urls-list (`GameEndpoints.GetSitemap`) — `/tank-till-tusen`
- [x] `wwwroot/llms.txt` — Tänk Till Tusen section

## Verification
- [x] `emlang lint` OK
- [x] `dotnet build` clean; `dotnet test` all green (211 total)
- [x] `Grep -i gratis src` → 0 hits; no em dash (—) in svensk copy
- [x] E2E (two cookie jars over HTTP): catalog → new game → 2nd player joins → start → puzzle
      (tal/mål/45 s clock) → both submit → score gear closes round → round results (sample
      solution + per-player reached/score/total) → 5 rounds → final standings (tie shares the
      win, lowest total wins). Deadline/non-submitter→100 + exact→−10 covered by DeciderTests.

## Difficulty nivåer (familj | klassisk | svår)
- [x] Spec: `difficulty` on OpenLobby + LobbyOpened, DIFFICULTY comment (knob = minsta antal
      steg till målet), catalog = 3 rader; `emlang lint` OK
- [x] Domain: `Difficulty` enum; Solver.Reachable keeps SHORTEST route per value;
      PuzzleGenerator.Generate(difficulty, ...) filters targets (familj <= 2, klassisk allt,
      svår >= 4 steg); TankContext.GeneratePuzzles takes Difficulty
- [x] Tests: shortest-route self-check + familj/svår step-count theories + updated fixtures
- [x] Web: TankCatalog 3 nivå-rader → /new?difficulty=..., TankHostForm hidden field,
      PostOpen/GetNew ParseDifficulty (unknown → Klassisk); build + test green (224 total)
